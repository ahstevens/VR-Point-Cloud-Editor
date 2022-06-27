/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;

using LPoint = OnlineMapsVectorTile.LPoint;
using LPoints = System.Collections.Generic.List<OnlineMapsVectorTile.LPoint>;

public class OnlineMapsMapboxClipper
{
    public class PolyNode
    {
        internal LPoints polygon = new LPoints();
        internal List<PolyNode> children = new List<PolyNode>();
        public bool isOpen;
    }

    public class PolyTree : PolyNode
    {
        internal List<PolyNode> polygons = new List<PolyNode>();

        public void Clear()
        {
            for (int i = 0; i < polygons.Count; i++) polygons[i] = null;
            polygons.Clear();
            children.Clear();
        }

        public int Total
        {
            get
            {
                int result = polygons.Count;
                if (result > 0 && children[0] != polygons[0]) result--;
                return result;
            }
        }
    }

    public enum ClipType { ctIntersection, ctUnion, ctDifference, ctXor };
    public enum PolyType { ptSubject, ptClip };
    public enum PolyFillType { pftEvenOdd, pftNonZero, pftPositive, pftNegative };
    internal enum EdgeSide { esLeft, esRight };
    internal enum Direction { dRightToLeft, dLeftToRight };

    internal class TEdge
    {
        internal LPoint Bot;
        internal LPoint Curr;
        internal LPoint Top;
        internal LPoint Delta;
        internal double Dx;
        internal PolyType PolyTyp;
        internal EdgeSide Side;
        internal int WindDelta;
        internal int WindCnt;
        internal int WindCnt2;
        internal int OutIdx;
        internal TEdge Next;
        internal TEdge Prev;
        internal TEdge NextInLML;
        internal TEdge NextInAEL;
        internal TEdge PrevInAEL;
        internal TEdge NextInSEL;
        internal TEdge PrevInSEL;
    };

    public class IntersectNode
    {
        internal TEdge Edge1;
        internal TEdge Edge2;
        internal LPoint Pt;
    };

    internal class LocalMinima
    {
        internal long Y;
        internal TEdge LeftBound;
        internal TEdge RightBound;
        internal LocalMinima Next;
    };

    internal class Scanbeam
    {
        internal long Y;
        internal Scanbeam Next;
    };

    internal class Maxima
    {
        internal long X;
        internal Maxima Next;
        internal Maxima Prev;
    };

    internal class OutRec
    {
        internal int Idx;
        internal bool IsHole;
        internal bool IsOpen;
        internal OutRec FirstLeft;
        internal OutPt Pts;
        internal OutPt BottomPt;
        internal PolyNode PolyNode;
    };

    internal class OutPt
    {
        internal int Idx;
        internal LPoint Pt;
        internal OutPt Next;
        internal OutPt Prev;
    };

    internal class Join
    {
        internal OutPt OutPt1;
        internal OutPt OutPt2;
        internal LPoint OffPt;
    };

    public class Clipper
    {
        public const int ioReverseSolution = 1;
        public const int ioStrictlySimple = 2;
        public const int ioPreserveCollinear = 4;

        private ClipType m_ClipType;
        private Maxima m_Maxima;
        private TEdge m_SortedEdges;
        private List<IntersectNode> m_IntersectList;
        private bool m_ExecuteLocked;
        private PolyFillType m_ClipFillType;
        private PolyFillType m_SubjFillType;
        private List<Join> m_Joins;
        private List<Join> m_GhostJoins;
        private bool m_UsingPolyTree;
        internal const double horizontal = -3.4E+38;
        internal const int Skip = -2;
        internal const int Unassigned = -1;
        internal LocalMinima m_MinimaList;
        internal LocalMinima m_CurrentLM;
        internal Scanbeam m_Scanbeam;
        internal List<OutRec> m_PolyOuts;
        internal TEdge m_ActiveEdges;
        internal bool m_HasOpenPaths;

        public Clipper(int InitOptions = 0)
        {
            m_Scanbeam = null;
            m_Maxima = null;
            m_ActiveEdges = null;
            m_SortedEdges = null;
            m_IntersectList = new List<IntersectNode>();
            m_ExecuteLocked = false;
            m_UsingPolyTree = false;
            m_PolyOuts = new List<OutRec>();
            m_Joins = new List<Join>();
            m_GhostJoins = new List<Join>();
            ReverseSolution = (ioReverseSolution & InitOptions) != 0;
            StrictlySimple = (ioStrictlySimple & InitOptions) != 0;
            PreserveCollinear = (ioPreserveCollinear & InitOptions) != 0;
        }

        public bool PreserveCollinear { get; set; }

        public void Swap(ref long val1, ref long val2)
        {
            long tmp = val1;
            val1 = val2;
            val2 = tmp;
        }

        internal static bool IsHorizontal(TEdge e)
        {
            return e.Delta.y == 0;
        }

        internal static bool SlopesEqual(TEdge e1, TEdge e2)
        {
            return e1.Delta.y * e2.Delta.x == e1.Delta.x * e2.Delta.y;
        }

        internal static bool SlopesEqual(LPoint pt1, LPoint pt2, LPoint pt3)
        {
            return (pt1.y - pt2.y) * (pt2.x - pt3.x) - (pt1.x - pt2.x) * (pt2.y - pt3.y) == 0;
        }

        internal static bool SlopesEqual(LPoint pt1, LPoint pt2, LPoint pt3, LPoint pt4)
        {
            return (pt1.y - pt2.y) * (pt3.x - pt4.x) - (pt1.x - pt2.x) * (pt3.y - pt4.y) == 0;
        }

        private void InitEdge(TEdge e, TEdge eNext, TEdge ePrev, LPoint pt)
        {
            e.Next = eNext;
            e.Prev = ePrev;
            e.Curr = pt;
            e.OutIdx = Unassigned;
        }

        private void InitEdge2(TEdge e, PolyType polyType)
        {
            if (e.Curr.y >= e.Next.Curr.y)
            {
                e.Bot = e.Curr;
                e.Top = e.Next.Curr;
            }
            else
            {
                e.Top = e.Curr;
                e.Bot = e.Next.Curr;
            }
            SetDx(e);
            e.PolyTyp = polyType;
        }

        private TEdge FindNextLocMin(TEdge E)
        {
            while (true)
            {
                while (E.Bot != E.Prev.Bot || E.Curr == E.Top) E = E.Next;
                if (E.Dx != horizontal && E.Prev.Dx != horizontal) break;
                while (E.Prev.Dx == horizontal) E = E.Prev;
                TEdge E2 = E;
                while (E.Dx == horizontal) E = E.Next;
                if (E.Top.y == E.Prev.Bot.y) continue;
                if (E2.Prev.Bot.x < E.Bot.x) E = E2;
                break;
            }
            return E;
        }

        private TEdge ProcessBound(TEdge E, bool LeftBoundIsForward)
        {
            TEdge EStart, Result = E;
            TEdge Horz;

            if (Result.OutIdx == Skip)
            {
                E = Result;
                if (LeftBoundIsForward)
                {
                    while (E.Top.y == E.Next.Bot.y) E = E.Next;
                    while (E != Result && E.Dx == horizontal) E = E.Prev;
                }
                else
                {
                    while (E.Top.y == E.Prev.Bot.y) E = E.Prev;
                    while (E != Result && E.Dx == horizontal) E = E.Next;
                }
                if (E == Result)
                {
                    Result = LeftBoundIsForward ? E.Next : E.Prev;
                }
                else
                {
                    E = LeftBoundIsForward ? Result.Next : Result.Prev;
                    LocalMinima locMin = new LocalMinima
                    {
                        Next = null,
                        Y = E.Bot.y,
                        LeftBound = null,
                        RightBound = E
                    };
                    E.WindDelta = 0;
                    Result = ProcessBound(E, LeftBoundIsForward);
                    InsertLocalMinima(locMin);
                }
                return Result;
            }

            if (E.Dx == horizontal)
            {
                if (LeftBoundIsForward) EStart = E.Prev;
                else EStart = E.Next;
                if (EStart.Dx == horizontal)
                {
                    if (EStart.Bot.x != E.Bot.x && EStart.Top.x != E.Bot.x) ReverseHorizontal(E);
                }
                else if (EStart.Bot.x != E.Bot.x) ReverseHorizontal(E);
            }

            EStart = E;
            if (LeftBoundIsForward)
            {
                while (Result.Top.y == Result.Next.Bot.y && Result.Next.OutIdx != Skip) Result = Result.Next;
                if (Result.Dx == horizontal && Result.Next.OutIdx != Skip)
                {
                    Horz = Result;
                    while (Horz.Prev.Dx == horizontal) Horz = Horz.Prev;
                    if (Horz.Prev.Top.x > Result.Next.Top.x) Result = Horz.Prev;
                }
                while (E != Result)
                {
                    E.NextInLML = E.Next;
                    if (E.Dx == horizontal && E != EStart && E.Bot.x != E.Prev.Top.x) ReverseHorizontal(E);
                    E = E.Next;
                }
                if (E.Dx == horizontal && E != EStart && E.Bot.x != E.Prev.Top.x) ReverseHorizontal(E);
                Result = Result.Next;
            }
            else
            {
                while (Result.Top.y == Result.Prev.Bot.y && Result.Prev.OutIdx != Skip) Result = Result.Prev;
                if (Result.Dx == horizontal && Result.Prev.OutIdx != Skip)
                {
                    Horz = Result;
                    while (Horz.Next.Dx == horizontal) Horz = Horz.Next;
                    if (Horz.Next.Top.x == Result.Prev.Top.x || Horz.Next.Top.x > Result.Prev.Top.x) Result = Horz.Next;
                }

                while (E != Result)
                {
                    E.NextInLML = E.Prev;
                    if (E.Dx == horizontal && E != EStart && E.Bot.x != E.Next.Top.x) ReverseHorizontal(E);
                    E = E.Prev;
                }
                if (E.Dx == horizontal && E != EStart && E.Bot.x != E.Next.Top.x) ReverseHorizontal(E);
                Result = Result.Prev;
            }
            return Result;
        }

        public bool AddPath(LPoints pg, PolyType polyType, bool Closed)
        {
            int highI = pg.Count - 1;
            if (Closed)
            {
                while (highI > 0 && pg[highI] == pg[0]) --highI;
            }
            while (highI > 0 && pg[highI] == pg[highI - 1]) --highI;
            if (Closed && highI < 2 || !Closed && highI < 1) return false;

            List<TEdge> edges = new List<TEdge>(highI + 1);
            for (int i = 0; i <= highI; i++) edges.Add(new TEdge());

            bool IsFlat = true;

            edges[1].Curr = pg[1];
            InitEdge(edges[0], edges[1], edges[highI], pg[0]);
            InitEdge(edges[highI], edges[0], edges[highI - 1], pg[highI]);
            for (int i = highI - 1; i >= 1; --i) InitEdge(edges[i], edges[i + 1], edges[i - 1], pg[i]);
            TEdge eStart = edges[0];

            TEdge E = eStart, eLoopStop = eStart;
            while (true)
            {
                if (E.Curr == E.Next.Curr && (Closed || E.Next != eStart))
                {
                    if (E == E.Next) break;
                    if (E == eStart) eStart = E.Next;
                    E = RemoveEdge(E);
                    eLoopStop = E;
                    continue;
                }
                if (E.Prev == E.Next) break;
                if (Closed && SlopesEqual(E.Prev.Curr, E.Curr, E.Next.Curr) &&
                   (!PreserveCollinear || !Pt2IsBetweenPt1AndPt3(E.Prev.Curr, E.Curr, E.Next.Curr)))
                {
                    if (E == eStart) eStart = E.Next;
                    E = RemoveEdge(E);
                    E = E.Prev;
                    eLoopStop = E;
                    continue;
                }
                E = E.Next;
                if (E == eLoopStop || !Closed && E.Next == eStart) break;
            }

            if (!Closed && E == E.Next || Closed && E.Prev == E.Next) return false;

            if (!Closed)
            {
                m_HasOpenPaths = true;
                eStart.Prev.OutIdx = Skip;
            }

            E = eStart;
            do
            {
                InitEdge2(E, polyType);
                E = E.Next;
                if (IsFlat && E.Curr.y != eStart.Curr.y) IsFlat = false;
            }
            while (E != eStart);

            if (IsFlat)
            {
                if (Closed) return false;
                E.Prev.OutIdx = Skip;
                LocalMinima locMin = new LocalMinima
                {
                    Next = null,
                    Y = E.Bot.y,
                    LeftBound = null,
                    RightBound = E
                };
                locMin.RightBound.Side = EdgeSide.esRight;
                locMin.RightBound.WindDelta = 0;
                while (true)
                {
                    if (E.Bot.x != E.Prev.Top.x) ReverseHorizontal(E);
                    if (E.Next.OutIdx == Skip) break;
                    E.NextInLML = E.Next;
                    E = E.Next;
                }
                InsertLocalMinima(locMin);
                return true;
            }

            TEdge EMin = null;

            if (E.Prev.Bot == E.Prev.Top) E = E.Next;

            while (true)
            {
                E = FindNextLocMin(E);
                if (E == EMin) break;
                if (EMin == null) EMin = E;

                LocalMinima locMin = new LocalMinima
                {
                    Next = null,
                    Y = E.Bot.y
                };
                bool leftBoundIsForward;
                if (E.Dx < E.Prev.Dx)
                {
                    locMin.LeftBound = E.Prev;
                    locMin.RightBound = E;
                    leftBoundIsForward = false;
                }
                else
                {
                    locMin.LeftBound = E;
                    locMin.RightBound = E.Prev;
                    leftBoundIsForward = true;
                }
                locMin.LeftBound.Side = EdgeSide.esLeft;
                locMin.RightBound.Side = EdgeSide.esRight;

                if (!Closed) locMin.LeftBound.WindDelta = 0;
                else if (locMin.LeftBound.Next == locMin.RightBound) locMin.LeftBound.WindDelta = -1;
                else locMin.LeftBound.WindDelta = 1;
                locMin.RightBound.WindDelta = -locMin.LeftBound.WindDelta;

                E = ProcessBound(locMin.LeftBound, leftBoundIsForward);
                if (E.OutIdx == Skip) E = ProcessBound(E, leftBoundIsForward);

                TEdge E2 = ProcessBound(locMin.RightBound, !leftBoundIsForward);
                if (E2.OutIdx == Skip) E2 = ProcessBound(E2, !leftBoundIsForward);

                if (locMin.LeftBound.OutIdx == Skip) locMin.LeftBound = null;
                else if (locMin.RightBound.OutIdx == Skip) locMin.RightBound = null;
                InsertLocalMinima(locMin);
                if (!leftBoundIsForward) E = E2;
            }
            return true;

        }

        public void AddPaths(List<LPoints> ppg, PolyType polyType, bool closed)
        {
            for (int i = 0; i < ppg.Count; ++i) AddPath(ppg[i], polyType, closed);
        }

        internal bool Pt2IsBetweenPt1AndPt3(LPoint pt1, LPoint pt2, LPoint pt3)
        {
            if (pt1 == pt3 || pt1 == pt2 || pt3 == pt2) return false;
            if (pt1.x != pt3.x) return pt2.x > pt1.x == pt2.x < pt3.x;
            return pt2.y > pt1.y == pt2.y < pt3.y;
        }

        TEdge RemoveEdge(TEdge e)
        {
            e.Prev.Next = e.Next;
            e.Next.Prev = e.Prev;
            TEdge result = e.Next;
            e.Prev = null;
            return result;
        }

        private void SetDx(TEdge e)
        {
            e.Delta.x = e.Top.x - e.Bot.x;
            e.Delta.y = e.Top.y - e.Bot.y;
            if (e.Delta.y == 0) e.Dx = horizontal;
            else e.Dx = (double)e.Delta.x / e.Delta.y;
        }

        private void InsertLocalMinima(LocalMinima newLm)
        {
            if (m_MinimaList == null) m_MinimaList = newLm;
            else if (newLm.Y >= m_MinimaList.Y)
            {
                newLm.Next = m_MinimaList;
                m_MinimaList = newLm;
            }
            else
            {
                LocalMinima tmpLm = m_MinimaList;
                while (tmpLm.Next != null && newLm.Y < tmpLm.Next.Y) tmpLm = tmpLm.Next;
                newLm.Next = tmpLm.Next;
                tmpLm.Next = newLm;
            }
        }

        internal bool PopLocalMinima(long Y, out LocalMinima current)
        {
            current = m_CurrentLM;
            if (m_CurrentLM != null && m_CurrentLM.Y == Y)
            {
                m_CurrentLM = m_CurrentLM.Next;
                return true;
            }
            return false;
        }

        private void ReverseHorizontal(TEdge e)
        {
            Swap(ref e.Top.x, ref e.Bot.x);
        }

        protected void Reset()
        {
            m_CurrentLM = m_MinimaList;
            if (m_CurrentLM == null) return;

            m_Scanbeam = null;
            LocalMinima lm = m_MinimaList;
            while (lm != null)
            {
                InsertScanbeam(lm.Y);
                TEdge e = lm.LeftBound;
                if (e != null)
                {
                    e.Curr = e.Bot;
                    e.OutIdx = Unassigned;
                }
                e = lm.RightBound;
                if (e != null)
                {
                    e.Curr = e.Bot;
                    e.OutIdx = Unassigned;
                }
                lm = lm.Next;
            }
            m_ActiveEdges = null;
        }

        internal void InsertScanbeam(long Y)
        {
            if (m_Scanbeam == null)
            {
                m_Scanbeam = new Scanbeam
                {
                    Next = null,
                    Y = Y
                };
            }
            else if (Y > m_Scanbeam.Y)
            {
                Scanbeam newSb = new Scanbeam
                {
                    Y = Y,
                    Next = m_Scanbeam
                };
                m_Scanbeam = newSb;
            }
            else
            {
                Scanbeam sb2 = m_Scanbeam;
                while (sb2.Next != null && Y <= sb2.Next.Y) sb2 = sb2.Next;
                if (Y == sb2.Y) return;
                Scanbeam newSb = new Scanbeam
                {
                    Y = Y,
                    Next = sb2.Next
                };
                sb2.Next = newSb;
            }
        }

        internal bool PopScanbeam(out long Y)
        {
            if (m_Scanbeam == null)
            {
                Y = 0;
                return false;
            }
            Y = m_Scanbeam.Y;
            m_Scanbeam = m_Scanbeam.Next;
            return true;
        }

        internal OutRec CreateOutRec()
        {
            OutRec result = new OutRec
            {
                Idx = Unassigned,
                IsHole = false,
                IsOpen = false,
                FirstLeft = null,
                Pts = null,
                BottomPt = null,
                PolyNode = null
            };
            m_PolyOuts.Add(result);
            result.Idx = m_PolyOuts.Count - 1;
            return result;
        }

        internal void UpdateEdgeIntoAEL(ref TEdge e)
        {
            TEdge AelPrev = e.PrevInAEL;
            TEdge AelNext = e.NextInAEL;
            e.NextInLML.OutIdx = e.OutIdx;
            if (AelPrev != null) AelPrev.NextInAEL = e.NextInLML;
            else m_ActiveEdges = e.NextInLML;
            if (AelNext != null) AelNext.PrevInAEL = e.NextInLML;
            e.NextInLML.Side = e.Side;
            e.NextInLML.WindDelta = e.WindDelta;
            e.NextInLML.WindCnt = e.WindCnt;
            e.NextInLML.WindCnt2 = e.WindCnt2;
            e = e.NextInLML;
            e.Curr = e.Bot;
            e.PrevInAEL = AelPrev;
            e.NextInAEL = AelNext;
            if (!IsHorizontal(e)) InsertScanbeam(e.Top.y);
        }

        internal void SwapPositionsInAEL(TEdge edge1, TEdge edge2)
        {
            if (edge1.NextInAEL == edge1.PrevInAEL || edge2.NextInAEL == edge2.PrevInAEL) return;

            if (edge1.NextInAEL == edge2)
            {
                TEdge next = edge2.NextInAEL;
                if (next != null) next.PrevInAEL = edge1;
                TEdge prev = edge1.PrevInAEL;
                if (prev != null) prev.NextInAEL = edge2;
                edge2.PrevInAEL = prev;
                edge2.NextInAEL = edge1;
                edge1.PrevInAEL = edge2;
                edge1.NextInAEL = next;
            }
            else if (edge2.NextInAEL == edge1)
            {
                TEdge next = edge1.NextInAEL;
                if (next != null) next.PrevInAEL = edge2;
                TEdge prev = edge2.PrevInAEL;
                if (prev != null) prev.NextInAEL = edge1;
                edge1.PrevInAEL = prev;
                edge1.NextInAEL = edge2;
                edge2.PrevInAEL = edge1;
                edge2.NextInAEL = next;
            }
            else
            {
                TEdge next = edge1.NextInAEL;
                TEdge prev = edge1.PrevInAEL;
                edge1.NextInAEL = edge2.NextInAEL;
                if (edge1.NextInAEL != null) edge1.NextInAEL.PrevInAEL = edge1;
                edge1.PrevInAEL = edge2.PrevInAEL;
                if (edge1.PrevInAEL != null) edge1.PrevInAEL.NextInAEL = edge1;
                edge2.NextInAEL = next;
                if (edge2.NextInAEL != null) edge2.NextInAEL.PrevInAEL = edge2;
                edge2.PrevInAEL = prev;
                if (edge2.PrevInAEL != null) edge2.PrevInAEL.NextInAEL = edge2;
            }

            if (edge1.PrevInAEL == null) m_ActiveEdges = edge1;
            else if (edge2.PrevInAEL == null) m_ActiveEdges = edge2;
        }

        internal void DeleteFromAEL(TEdge e)
        {
            TEdge AelPrev = e.PrevInAEL;
            TEdge AelNext = e.NextInAEL;
            if (AelPrev == null && AelNext == null && e != m_ActiveEdges) return;
            if (AelPrev != null) AelPrev.NextInAEL = AelNext;
            else m_ActiveEdges = AelNext;
            if (AelNext != null) AelNext.PrevInAEL = AelPrev;
            e.NextInAEL = null;
            e.PrevInAEL = null;
        }

        private void InsertMaxima(long X)
        {
            Maxima newMax = new Maxima { X = X };
            if (m_Maxima == null)
            {
                m_Maxima = newMax;
                m_Maxima.Next = null;
                m_Maxima.Prev = null;
            }
            else if (X < m_Maxima.X)
            {
                newMax.Next = m_Maxima;
                newMax.Prev = null;
                m_Maxima = newMax;
            }
            else
            {
                Maxima m = m_Maxima;
                while (m.Next != null && X >= m.Next.X) m = m.Next;
                if (X == m.X) return;
                newMax.Next = m.Next;
                newMax.Prev = m;
                if (m.Next != null) m.Next.Prev = newMax;
                m.Next = newMax;
            }
        }

        public bool ReverseSolution;

        public bool StrictlySimple;

        public bool Execute(ClipType clipType, List<LPoints> solution, PolyFillType subjFillType, PolyFillType clipFillType)
        {
            if (m_ExecuteLocked) return false;
            if (m_HasOpenPaths) throw new Exception("Error: PolyTree struct is needed for open path clipping.");

            m_ExecuteLocked = true;
            solution.Clear();
            m_SubjFillType = subjFillType;
            m_ClipFillType = clipFillType;
            m_ClipType = clipType;
            m_UsingPolyTree = false;
            bool succeeded;
            try
            {
                succeeded = ExecuteInternal();
                if (succeeded) BuildResult(solution);
            }
            finally
            {
                DisposeAllPolyPts();
                m_ExecuteLocked = false;
            }
            return succeeded;
        }

        public bool Execute(ClipType clipType, PolyTree polytree, PolyFillType subjFillType, PolyFillType clipFillType)
        {
            if (m_ExecuteLocked) return false;
            m_ExecuteLocked = true;
            m_SubjFillType = subjFillType;
            m_ClipFillType = clipFillType;
            m_ClipType = clipType;
            m_UsingPolyTree = true;
            bool succeeded;
            try
            {
                succeeded = ExecuteInternal();
                if (succeeded) BuildResult2(polytree);
            }
            finally
            {
                DisposeAllPolyPts();
                m_ExecuteLocked = false;
            }
            return succeeded;
        }

        internal void FixHoleLinkage(OutRec outRec)
        {
            if (outRec.FirstLeft == null ||
                outRec.IsHole != outRec.FirstLeft.IsHole && outRec.FirstLeft.Pts != null) return;

            OutRec orfl = outRec.FirstLeft;
            while (orfl != null && (orfl.IsHole == outRec.IsHole || orfl.Pts == null)) orfl = orfl.FirstLeft;
            outRec.FirstLeft = orfl;
        }

        private bool ExecuteInternal()
        {
            try
            {
                Reset();
                m_SortedEdges = null;
                m_Maxima = null;

                long botY, topY;
                if (!PopScanbeam(out botY)) return false;
                InsertLocalMinimaIntoAEL(botY);
                while (PopScanbeam(out topY) || m_CurrentLM != null)
                {
                    ProcessHorizontals();
                    m_GhostJoins.Clear();
                    if (!ProcessIntersections(topY)) return false;
                    ProcessEdgesAtTopOfScanbeam(topY);
                    botY = topY;
                    InsertLocalMinimaIntoAEL(botY);
                }

                foreach (OutRec outRec in m_PolyOuts)
                {
                    if (outRec.Pts == null || outRec.IsOpen) continue;
                    if ((outRec.IsHole ^ ReverseSolution) == Area(outRec) > 0) ReversePolyPtLinks(outRec.Pts);
                }

                JoinCommonEdges();

                foreach (OutRec outRec in m_PolyOuts)
                {
                    if (outRec.Pts == null) continue;
                    if (outRec.IsOpen) FixupOutPolyline(outRec);
                    else FixupOutPolygon(outRec);
                }

                if (StrictlySimple) DoSimplePolygons();
                return true;
            }
            finally
            {
                m_Joins.Clear();
                m_GhostJoins.Clear();
            }
        }

        private void DisposeAllPolyPts()
        {
            for (int i = 0; i < m_PolyOuts.Count; ++i)
            {
                OutRec outRec = m_PolyOuts[i];
                outRec.Pts = null;
            }
            m_PolyOuts.Clear();
        }

        private void AddJoin(OutPt Op1, OutPt Op2, LPoint OffPt)
        {
            m_Joins.Add(new Join
            {
                OutPt1 = Op1,
                OutPt2 = Op2,
                OffPt = OffPt
            });
        }

        private void AddGhostJoin(OutPt Op, LPoint OffPt)
        {
            m_GhostJoins.Add(new Join
            {
                OutPt1 = Op,
                OffPt = OffPt
            });
        }

        private void InsertLocalMinimaIntoAEL(long botY)
        {
            LocalMinima lm;
            while (PopLocalMinima(botY, out lm))
            {
                TEdge lb = lm.LeftBound;
                TEdge rb = lm.RightBound;

                OutPt Op1 = null;
                if (lb == null)
                {
                    InsertEdgeIntoAEL(rb, null);
                    SetWindingCount(rb);
                    if (IsContributing(rb)) Op1 = AddOutPt(rb, rb.Bot);
                }
                else if (rb == null)
                {
                    InsertEdgeIntoAEL(lb, null);
                    SetWindingCount(lb);
                    if (IsContributing(lb)) Op1 = AddOutPt(lb, lb.Bot);
                    InsertScanbeam(lb.Top.y);
                }
                else
                {
                    InsertEdgeIntoAEL(lb, null);
                    InsertEdgeIntoAEL(rb, lb);
                    SetWindingCount(lb);
                    rb.WindCnt = lb.WindCnt;
                    rb.WindCnt2 = lb.WindCnt2;
                    if (IsContributing(lb)) Op1 = AddLocalMinPoly(lb, rb, lb.Bot);
                    InsertScanbeam(lb.Top.y);
                }

                if (rb != null)
                {
                    if (IsHorizontal(rb))
                    {
                        if (rb.NextInLML != null) InsertScanbeam(rb.NextInLML.Top.y);
                        AddEdgeToSEL(rb);
                    }
                    else InsertScanbeam(rb.Top.y);
                }

                if (lb == null || rb == null) continue;

                if (Op1 != null && IsHorizontal(rb) && m_GhostJoins.Count > 0 && rb.WindDelta != 0)
                {
                    for (int i = 0; i < m_GhostJoins.Count; i++)
                    {
                        Join j = m_GhostJoins[i];
                        if (HorzSegmentsOverlap(j.OutPt1.Pt.x, j.OffPt.x, rb.Bot.x, rb.Top.x)) AddJoin(j.OutPt1, Op1, j.OffPt);
                    }
                }

                if (lb.OutIdx >= 0 && lb.PrevInAEL != null &&
                  lb.PrevInAEL.Curr.x == lb.Bot.x &&
                  lb.PrevInAEL.OutIdx >= 0 &&
                  SlopesEqual(lb.PrevInAEL.Curr, lb.PrevInAEL.Top, lb.Curr, lb.Top) &&
                  lb.WindDelta != 0 && lb.PrevInAEL.WindDelta != 0)
                {
                    OutPt Op2 = AddOutPt(lb.PrevInAEL, lb.Bot);
                    AddJoin(Op1, Op2, lb.Top);
                }

                if (lb.NextInAEL != rb)
                {
                    if (rb.OutIdx >= 0 && rb.PrevInAEL.OutIdx >= 0 &&
                      SlopesEqual(rb.PrevInAEL.Curr, rb.PrevInAEL.Top, rb.Curr, rb.Top) &&
                      rb.WindDelta != 0 && rb.PrevInAEL.WindDelta != 0)
                    {
                        OutPt Op2 = AddOutPt(rb.PrevInAEL, rb.Bot);
                        AddJoin(Op1, Op2, rb.Top);
                    }

                    TEdge e = lb.NextInAEL;
                    if (e != null)
                    {
                        while (e != rb)
                        {
                            IntersectEdges(rb, e, lb.Curr);
                            e = e.NextInAEL;
                        }
                    }
                }
            }
        }

        private void InsertEdgeIntoAEL(TEdge edge, TEdge startEdge)
        {
            if (m_ActiveEdges == null)
            {
                edge.PrevInAEL = null;
                edge.NextInAEL = null;
                m_ActiveEdges = edge;
            }
            else if (startEdge == null && E2InsertsBeforeE1(m_ActiveEdges, edge))
            {
                edge.PrevInAEL = null;
                edge.NextInAEL = m_ActiveEdges;
                m_ActiveEdges.PrevInAEL = edge;
                m_ActiveEdges = edge;
            }
            else
            {
                if (startEdge == null) startEdge = m_ActiveEdges;
                while (startEdge.NextInAEL != null && !E2InsertsBeforeE1(startEdge.NextInAEL, edge)) startEdge = startEdge.NextInAEL;
                edge.NextInAEL = startEdge.NextInAEL;
                if (startEdge.NextInAEL != null) startEdge.NextInAEL.PrevInAEL = edge;
                edge.PrevInAEL = startEdge;
                startEdge.NextInAEL = edge;
            }
        }

        private bool E2InsertsBeforeE1(TEdge e1, TEdge e2)
        {
            if (e2.Curr.x == e1.Curr.x)
            {
                if (e2.Top.y > e1.Top.y) return e2.Top.x < TopX(e1, e2.Top.y);
                return e1.Top.x > TopX(e2, e1.Top.y);
            }
            return e2.Curr.x < e1.Curr.x;
        }

        private bool IsEvenOddFillType(TEdge edge)
        {
            if (edge.PolyTyp == PolyType.ptSubject) return m_SubjFillType == PolyFillType.pftEvenOdd;
            return m_ClipFillType == PolyFillType.pftEvenOdd;
        }

        private bool IsEvenOddAltFillType(TEdge edge)
        {
            if (edge.PolyTyp == PolyType.ptSubject) return m_ClipFillType == PolyFillType.pftEvenOdd;
            return m_SubjFillType == PolyFillType.pftEvenOdd;
        }

        private bool IsContributing(TEdge edge)
        {
            PolyFillType pft, pft2;
            if (edge.PolyTyp == PolyType.ptSubject)
            {
                pft = m_SubjFillType;
                pft2 = m_ClipFillType;
            }
            else
            {
                pft = m_ClipFillType;
                pft2 = m_SubjFillType;
            }

            switch (pft)
            {
                case PolyFillType.pftEvenOdd:
                    if (edge.WindDelta == 0 && edge.WindCnt != 1) return false;
                    break;
                case PolyFillType.pftNonZero:
                    if (Math.Abs(edge.WindCnt) != 1) return false;
                    break;
                case PolyFillType.pftPositive:
                    if (edge.WindCnt != 1) return false;
                    break;
                default:
                    if (edge.WindCnt != -1) return false;
                    break;
            }

            switch (m_ClipType)
            {
                case ClipType.ctIntersection:
                    switch (pft2)
                    {
                        case PolyFillType.pftEvenOdd:
                        case PolyFillType.pftNonZero:
                            return edge.WindCnt2 != 0;
                        case PolyFillType.pftPositive:
                            return edge.WindCnt2 > 0;
                        default:
                            return edge.WindCnt2 < 0;
                    }
                case ClipType.ctUnion:
                    switch (pft2)
                    {
                        case PolyFillType.pftEvenOdd:
                        case PolyFillType.pftNonZero:
                            return edge.WindCnt2 == 0;
                        case PolyFillType.pftPositive:
                            return edge.WindCnt2 <= 0;
                        default:
                            return edge.WindCnt2 >= 0;
                    }
                case ClipType.ctDifference:
                    if (edge.PolyTyp == PolyType.ptSubject)
                    {
                        switch (pft2)
                        {
                            case PolyFillType.pftEvenOdd:
                            case PolyFillType.pftNonZero:
                                return edge.WindCnt2 == 0;
                            case PolyFillType.pftPositive:
                                return edge.WindCnt2 <= 0;
                            default:
                                return edge.WindCnt2 >= 0;
                        }
                    }
                    switch (pft2)
                    {
                        case PolyFillType.pftEvenOdd:
                        case PolyFillType.pftNonZero:
                            return edge.WindCnt2 != 0;
                        case PolyFillType.pftPositive:
                            return edge.WindCnt2 > 0;
                        default:
                            return edge.WindCnt2 < 0;
                    }
                case ClipType.ctXor:
                    if (edge.WindDelta == 0)
                    {
                        switch (pft2)
                        {
                            case PolyFillType.pftEvenOdd:
                            case PolyFillType.pftNonZero:
                                return edge.WindCnt2 == 0;
                            case PolyFillType.pftPositive:
                                return edge.WindCnt2 <= 0;
                            default:
                                return edge.WindCnt2 >= 0;
                        }
                    }
                    return true;
            }
            return true;
        }

        private void SetWindingCount(TEdge edge)
        {
            TEdge e = edge.PrevInAEL;
            while (e != null && (e.PolyTyp != edge.PolyTyp || e.WindDelta == 0)) e = e.PrevInAEL;
            if (e == null)
            {
                PolyFillType pft = edge.PolyTyp == PolyType.ptSubject ? m_SubjFillType : m_ClipFillType;
                if (edge.WindDelta == 0) edge.WindCnt = pft == PolyFillType.pftNegative ? -1 : 1;
                else edge.WindCnt = edge.WindDelta;
                edge.WindCnt2 = 0;
                e = m_ActiveEdges;
            }
            else if (edge.WindDelta == 0 && m_ClipType != ClipType.ctUnion)
            {
                edge.WindCnt = 1;
                edge.WindCnt2 = e.WindCnt2;
                e = e.NextInAEL;
            }
            else if (IsEvenOddFillType(edge))
            {
                if (edge.WindDelta == 0)
                {
                    bool Inside = true;
                    TEdge e2 = e.PrevInAEL;
                    while (e2 != null)
                    {
                        if (e2.PolyTyp == e.PolyTyp && e2.WindDelta != 0) Inside = !Inside;
                        e2 = e2.PrevInAEL;
                    }
                    edge.WindCnt = Inside ? 0 : 1;
                }
                else edge.WindCnt = edge.WindDelta;
                edge.WindCnt2 = e.WindCnt2;
                e = e.NextInAEL;
            }
            else
            {
                if (e.WindCnt * e.WindDelta < 0)
                {
                    if (Math.Abs(e.WindCnt) > 1)
                    {
                        if (e.WindDelta * edge.WindDelta < 0) edge.WindCnt = e.WindCnt;
                        else edge.WindCnt = e.WindCnt + edge.WindDelta;
                    }
                    else edge.WindCnt = edge.WindDelta == 0 ? 1 : edge.WindDelta;
                }
                else
                {
                    if (edge.WindDelta == 0) edge.WindCnt = e.WindCnt < 0 ? e.WindCnt - 1 : e.WindCnt + 1;
                    else if (e.WindDelta * edge.WindDelta < 0) edge.WindCnt = e.WindCnt;
                    else edge.WindCnt = e.WindCnt + edge.WindDelta;
                }
                edge.WindCnt2 = e.WindCnt2;
                e = e.NextInAEL;
            }

            if (IsEvenOddAltFillType(edge))
            {
                while (e != edge)
                {
                    if (e.WindDelta != 0) edge.WindCnt2 = edge.WindCnt2 == 0 ? 1 : 0;
                    e = e.NextInAEL;
                }
            }
            else
            {
                while (e != edge)
                {
                    edge.WindCnt2 += e.WindDelta;
                    e = e.NextInAEL;
                }
            }
        }

        private void AddEdgeToSEL(TEdge edge)
        {
            if (m_SortedEdges == null)
            {
                m_SortedEdges = edge;
                edge.PrevInSEL = null;
                edge.NextInSEL = null;
            }
            else
            {
                edge.NextInSEL = m_SortedEdges;
                edge.PrevInSEL = null;
                m_SortedEdges.PrevInSEL = edge;
                m_SortedEdges = edge;
            }
        }

        internal bool PopEdgeFromSEL(out TEdge e)
        {
            e = m_SortedEdges;
            if (e == null) return false;
            TEdge oldE = e;
            m_SortedEdges = e.NextInSEL;
            if (m_SortedEdges != null) m_SortedEdges.PrevInSEL = null;
            oldE.NextInSEL = null;
            oldE.PrevInSEL = null;
            return true;
        }

        private void SwapPositionsInSEL(TEdge edge1, TEdge edge2)
        {
            if (edge1.NextInSEL == null && edge1.PrevInSEL == null) return;
            if (edge2.NextInSEL == null && edge2.PrevInSEL == null) return;

            if (edge1.NextInSEL == edge2)
            {
                TEdge next = edge2.NextInSEL;
                if (next != null) next.PrevInSEL = edge1;
                TEdge prev = edge1.PrevInSEL;
                if (prev != null) prev.NextInSEL = edge2;
                edge2.PrevInSEL = prev;
                edge2.NextInSEL = edge1;
                edge1.PrevInSEL = edge2;
                edge1.NextInSEL = next;
            }
            else if (edge2.NextInSEL == edge1)
            {
                TEdge next = edge1.NextInSEL;
                if (next != null) next.PrevInSEL = edge2;
                TEdge prev = edge2.PrevInSEL;
                if (prev != null) prev.NextInSEL = edge1;
                edge1.PrevInSEL = prev;
                edge1.NextInSEL = edge2;
                edge2.PrevInSEL = edge1;
                edge2.NextInSEL = next;
            }
            else
            {
                TEdge next = edge1.NextInSEL;
                TEdge prev = edge1.PrevInSEL;
                edge1.NextInSEL = edge2.NextInSEL;
                if (edge1.NextInSEL != null) edge1.NextInSEL.PrevInSEL = edge1;
                edge1.PrevInSEL = edge2.PrevInSEL;
                if (edge1.PrevInSEL != null) edge1.PrevInSEL.NextInSEL = edge1;
                edge2.NextInSEL = next;
                if (edge2.NextInSEL != null) edge2.NextInSEL.PrevInSEL = edge2;
                edge2.PrevInSEL = prev;
                if (edge2.PrevInSEL != null) edge2.PrevInSEL.NextInSEL = edge2;
            }

            if (edge1.PrevInSEL == null) m_SortedEdges = edge1;
            else if (edge2.PrevInSEL == null) m_SortedEdges = edge2;
        }

        private void AddLocalMaxPoly(TEdge e1, TEdge e2, LPoint pt)
        {
            AddOutPt(e1, pt);
            if (e2.WindDelta == 0) AddOutPt(e2, pt);
            if (e1.OutIdx == e2.OutIdx)
            {
                e1.OutIdx = Unassigned;
                e2.OutIdx = Unassigned;
            }
            else if (e1.OutIdx < e2.OutIdx) AppendPolygon(e1, e2);
            else AppendPolygon(e2, e1);
        }

        private OutPt AddLocalMinPoly(TEdge e1, TEdge e2, LPoint pt)
        {
            OutPt result;
            TEdge e, prevE;
            if (IsHorizontal(e2) || e1.Dx > e2.Dx)
            {
                result = AddOutPt(e1, pt);
                e2.OutIdx = e1.OutIdx;
                e1.Side = EdgeSide.esLeft;
                e2.Side = EdgeSide.esRight;
                e = e1;
                prevE = e.PrevInAEL == e2 ? e2.PrevInAEL : e.PrevInAEL;
            }
            else
            {
                result = AddOutPt(e2, pt);
                e1.OutIdx = e2.OutIdx;
                e1.Side = EdgeSide.esRight;
                e2.Side = EdgeSide.esLeft;
                e = e2;
                prevE = e.PrevInAEL == e1 ? e1.PrevInAEL : e.PrevInAEL;
            }

            if (prevE != null && prevE.OutIdx >= 0)
            {
                long xPrev = TopX(prevE, pt.y);
                long xE = TopX(e, pt.y);
                if (xPrev == xE && e.WindDelta != 0 && prevE.WindDelta != 0 &&
                  SlopesEqual(new LPoint(xPrev, pt.y), prevE.Top, new LPoint(xE, pt.y), e.Top))
                {
                    OutPt outPt = AddOutPt(prevE, pt);
                    AddJoin(result, outPt, e.Top);
                }
            }
            return result;
        }

        private OutPt AddOutPt(TEdge e, LPoint pt)
        {
            if (e.OutIdx < 0)
            {
                OutRec outRec = CreateOutRec();
                outRec.IsOpen = e.WindDelta == 0;
                OutPt newOp = new OutPt();
                outRec.Pts = newOp;
                newOp.Idx = outRec.Idx;
                newOp.Pt = pt;
                newOp.Next = newOp;
                newOp.Prev = newOp;
                if (!outRec.IsOpen) SetHoleState(e, outRec);
                e.OutIdx = outRec.Idx;
                return newOp;
            }
            else
            {
                OutRec outRec = m_PolyOuts[e.OutIdx];
                OutPt op = outRec.Pts;
                bool ToFront = e.Side == EdgeSide.esLeft;
                if (ToFront && pt == op.Pt) return op;
                if (!ToFront && pt == op.Prev.Pt) return op.Prev;

                OutPt newOp = new OutPt
                {
                    Idx = outRec.Idx,
                    Pt = pt,
                    Next = op,
                    Prev = op.Prev
                };
                newOp.Prev.Next = newOp;
                op.Prev = newOp;
                if (ToFront) outRec.Pts = newOp;
                return newOp;
            }
        }

        private OutPt GetLastOutPt(TEdge e)
        {
            OutRec outRec = m_PolyOuts[e.OutIdx];
            if (e.Side == EdgeSide.esLeft) return outRec.Pts;
            return outRec.Pts.Prev;
        }

        private bool HorzSegmentsOverlap(long seg1a, long seg1b, long seg2a, long seg2b)
        {
            if (seg1a > seg1b) Swap(ref seg1a, ref seg1b);
            if (seg2a > seg2b) Swap(ref seg2a, ref seg2b);
            return seg1a < seg2b && seg2a < seg1b;
        }

        private void SetHoleState(TEdge e, OutRec outRec)
        {
            TEdge e2 = e.PrevInAEL;
            TEdge eTmp = null;
            while (e2 != null)
            {
                if (e2.OutIdx >= 0 && e2.WindDelta != 0)
                {
                    if (eTmp == null) eTmp = e2;
                    else if (eTmp.OutIdx == e2.OutIdx) eTmp = null;
                }
                e2 = e2.PrevInAEL;
            }

            if (eTmp == null)
            {
                outRec.FirstLeft = null;
                outRec.IsHole = false;
            }
            else
            {
                outRec.FirstLeft = m_PolyOuts[eTmp.OutIdx];
                outRec.IsHole = !outRec.FirstLeft.IsHole;
            }
        }

        private double GetDx(LPoint pt1, LPoint pt2)
        {
            if (pt1.y == pt2.y) return horizontal;
            return (double)(pt2.x - pt1.x) / (pt2.y - pt1.y);
        }

        private bool FirstIsBottomPt(OutPt btmPt1, OutPt btmPt2)
        {
            OutPt p = btmPt1.Prev;
            while (p.Pt == btmPt1.Pt && p != btmPt1) p = p.Prev;
            double dx1p = Math.Abs(GetDx(btmPt1.Pt, p.Pt));
            p = btmPt1.Next;
            while (p.Pt == btmPt1.Pt && p != btmPt1) p = p.Next;
            double dx1n = Math.Abs(GetDx(btmPt1.Pt, p.Pt));

            p = btmPt2.Prev;
            while (p.Pt == btmPt2.Pt && p != btmPt2) p = p.Prev;
            double dx2p = Math.Abs(GetDx(btmPt2.Pt, p.Pt));
            p = btmPt2.Next;
            while (p.Pt == btmPt2.Pt && p != btmPt2) p = p.Next;
            double dx2n = Math.Abs(GetDx(btmPt2.Pt, p.Pt));

            if (Math.Max(dx1p, dx1n) == Math.Max(dx2p, dx2n) && Math.Min(dx1p, dx1n) == Math.Min(dx2p, dx2n)) return Area(btmPt1) > 0;
            return dx1p >= dx2p && dx1p >= dx2n || dx1n >= dx2p && dx1n >= dx2n;
        }

        private OutPt GetBottomPt(OutPt pp)
        {
            OutPt dups = null;
            OutPt p = pp.Next;
            while (p != pp)
            {
                if (p.Pt.y > pp.Pt.y)
                {
                    pp = p;
                    dups = null;
                }
                else if (p.Pt.y == pp.Pt.y && p.Pt.x <= pp.Pt.x)
                {
                    if (p.Pt.x < pp.Pt.x)
                    {
                        dups = null;
                        pp = p;
                    }
                    else if (p.Next != pp && p.Prev != pp) dups = p;
                }
                p = p.Next;
            }
            if (dups != null)
            {
                while (dups != p)
                {
                    if (!FirstIsBottomPt(p, dups)) pp = dups;
                    dups = dups.Next;
                    while (dups.Pt != pp.Pt) dups = dups.Next;
                }
            }
            return pp;
        }

        private OutRec GetLowermostRec(OutRec outRec1, OutRec outRec2)
        {
            if (outRec1.BottomPt == null) outRec1.BottomPt = GetBottomPt(outRec1.Pts);
            if (outRec2.BottomPt == null) outRec2.BottomPt = GetBottomPt(outRec2.Pts);
            OutPt bPt1 = outRec1.BottomPt;
            OutPt bPt2 = outRec2.BottomPt;
            if (bPt1.Pt.y > bPt2.Pt.y) return outRec1;
            if (bPt1.Pt.y < bPt2.Pt.y) return outRec2;
            if (bPt1.Pt.x < bPt2.Pt.x) return outRec1;
            if (bPt1.Pt.x > bPt2.Pt.x) return outRec2;
            if (bPt1.Next == bPt1) return outRec2;
            if (bPt2.Next == bPt2) return outRec1;
            if (FirstIsBottomPt(bPt1, bPt2)) return outRec1;
            return outRec2;
        }

        bool OutRec1RightOfOutRec2(OutRec outRec1, OutRec outRec2)
        {
            do
            {
                outRec1 = outRec1.FirstLeft;
                if (outRec1 == outRec2) return true;
            } while (outRec1 != null);
            return false;
        }

        private OutRec GetOutRec(int idx)
        {
            OutRec outrec = m_PolyOuts[idx];
            while (outrec != m_PolyOuts[outrec.Idx]) outrec = m_PolyOuts[outrec.Idx];
            return outrec;
        }

        private void AppendPolygon(TEdge e1, TEdge e2)
        {
            OutRec outRec1 = m_PolyOuts[e1.OutIdx];
            OutRec outRec2 = m_PolyOuts[e2.OutIdx];

            OutRec holeStateRec;
            if (OutRec1RightOfOutRec2(outRec1, outRec2)) holeStateRec = outRec2;
            else if (OutRec1RightOfOutRec2(outRec2, outRec1)) holeStateRec = outRec1;
            else holeStateRec = GetLowermostRec(outRec1, outRec2);

            OutPt p1_lft = outRec1.Pts;
            OutPt p1_rt = p1_lft.Prev;
            OutPt p2_lft = outRec2.Pts;
            OutPt p2_rt = p2_lft.Prev;

            if (e1.Side == EdgeSide.esLeft)
            {
                if (e2.Side == EdgeSide.esLeft)
                {
                    ReversePolyPtLinks(p2_lft);
                    p2_lft.Next = p1_lft;
                    p1_lft.Prev = p2_lft;
                    p1_rt.Next = p2_rt;
                    p2_rt.Prev = p1_rt;
                    outRec1.Pts = p2_rt;
                }
                else
                {
                    p2_rt.Next = p1_lft;
                    p1_lft.Prev = p2_rt;
                    p2_lft.Prev = p1_rt;
                    p1_rt.Next = p2_lft;
                    outRec1.Pts = p2_lft;
                }
            }
            else
            {
                if (e2.Side == EdgeSide.esRight)
                {
                    ReversePolyPtLinks(p2_lft);
                    p1_rt.Next = p2_rt;
                    p2_rt.Prev = p1_rt;
                    p2_lft.Next = p1_lft;
                    p1_lft.Prev = p2_lft;
                }
                else
                {
                    p1_rt.Next = p2_lft;
                    p2_lft.Prev = p1_rt;
                    p1_lft.Prev = p2_rt;
                    p2_rt.Next = p1_lft;
                }
            }

            outRec1.BottomPt = null;
            if (holeStateRec == outRec2)
            {
                if (outRec2.FirstLeft != outRec1) outRec1.FirstLeft = outRec2.FirstLeft;
                outRec1.IsHole = outRec2.IsHole;
            }
            outRec2.Pts = null;
            outRec2.BottomPt = null;

            outRec2.FirstLeft = outRec1;

            int OKIdx = e1.OutIdx;
            int ObsoleteIdx = e2.OutIdx;

            e1.OutIdx = Unassigned;
            e2.OutIdx = Unassigned;

            TEdge e = m_ActiveEdges;
            while (e != null)
            {
                if (e.OutIdx == ObsoleteIdx)
                {
                    e.OutIdx = OKIdx;
                    e.Side = e1.Side;
                    break;
                }
                e = e.NextInAEL;
            }
            outRec2.Idx = outRec1.Idx;
        }

        private void ReversePolyPtLinks(OutPt pp)
        {
            if (pp == null) return;
            OutPt pp1 = pp;
            do
            {
                OutPt pp2 = pp1.Next;
                pp1.Next = pp1.Prev;
                pp1.Prev = pp2;
                pp1 = pp2;
            } while (pp1 != pp);
        }

        private static void SwapSides(TEdge edge1, TEdge edge2)
        {
            EdgeSide side = edge1.Side;
            edge1.Side = edge2.Side;
            edge2.Side = side;
        }

        private static void SwapPolyIndexes(TEdge edge1, TEdge edge2)
        {
            int outIdx = edge1.OutIdx;
            edge1.OutIdx = edge2.OutIdx;
            edge2.OutIdx = outIdx;
        }

        private void IntersectEdges(TEdge e1, TEdge e2, LPoint pt)
        {
            bool e1Contributing = e1.OutIdx >= 0;
            bool e2Contributing = e2.OutIdx >= 0;

            if (e1.WindDelta == 0 || e2.WindDelta == 0)
            {
                if (e1.WindDelta == 0 && e2.WindDelta == 0) return;
                if (e1.PolyTyp == e2.PolyTyp && e1.WindDelta != e2.WindDelta && m_ClipType == ClipType.ctUnion)
                {
                    if (e1.WindDelta == 0)
                    {
                        if (e2Contributing)
                        {
                            AddOutPt(e1, pt);
                            if (e1Contributing) e1.OutIdx = Unassigned;
                        }
                    }
                    else
                    {
                        if (e1Contributing)
                        {
                            AddOutPt(e2, pt);
                            if (e2Contributing) e2.OutIdx = Unassigned;
                        }
                    }
                }
                else if (e1.PolyTyp != e2.PolyTyp)
                {
                    if (e1.WindDelta == 0 && Math.Abs(e2.WindCnt) == 1 && (m_ClipType != ClipType.ctUnion || e2.WindCnt2 == 0))
                    {
                        AddOutPt(e1, pt);
                        if (e1Contributing) e1.OutIdx = Unassigned;
                    }
                    else if (e2.WindDelta == 0 && Math.Abs(e1.WindCnt) == 1 && (m_ClipType != ClipType.ctUnion || e1.WindCnt2 == 0))
                    {
                        AddOutPt(e2, pt);
                        if (e2Contributing) e2.OutIdx = Unassigned;
                    }
                }
                return;
            }

            if (e1.PolyTyp == e2.PolyTyp)
            {
                if (IsEvenOddFillType(e1))
                {
                    int oldE1WindCnt = e1.WindCnt;
                    e1.WindCnt = e2.WindCnt;
                    e2.WindCnt = oldE1WindCnt;
                }
                else
                {
                    if (e1.WindCnt + e2.WindDelta == 0) e1.WindCnt = -e1.WindCnt;
                    else e1.WindCnt += e2.WindDelta;
                    if (e2.WindCnt - e1.WindDelta == 0) e2.WindCnt = -e2.WindCnt;
                    else e2.WindCnt -= e1.WindDelta;
                }
            }
            else
            {
                if (!IsEvenOddFillType(e2)) e1.WindCnt2 += e2.WindDelta;
                else e1.WindCnt2 = e1.WindCnt2 == 0 ? 1 : 0;
                if (!IsEvenOddFillType(e1)) e2.WindCnt2 -= e1.WindDelta;
                else e2.WindCnt2 = e2.WindCnt2 == 0 ? 1 : 0;
            }

            PolyFillType e1FillType, e2FillType, e1FillType2, e2FillType2;
            if (e1.PolyTyp == PolyType.ptSubject)
            {
                e1FillType = m_SubjFillType;
                e1FillType2 = m_ClipFillType;
            }
            else
            {
                e1FillType = m_ClipFillType;
                e1FillType2 = m_SubjFillType;
            }
            if (e2.PolyTyp == PolyType.ptSubject)
            {
                e2FillType = m_SubjFillType;
                e2FillType2 = m_ClipFillType;
            }
            else
            {
                e2FillType = m_ClipFillType;
                e2FillType2 = m_SubjFillType;
            }

            int e1Wc, e2Wc;
            switch (e1FillType)
            {
                case PolyFillType.pftPositive:
                    e1Wc = e1.WindCnt;
                    break;
                case PolyFillType.pftNegative:
                    e1Wc = -e1.WindCnt;
                    break;
                default:
                    e1Wc = Math.Abs(e1.WindCnt);
                    break;
            }
            switch (e2FillType)
            {
                case PolyFillType.pftPositive:
                    e2Wc = e2.WindCnt;
                    break;
                case PolyFillType.pftNegative:
                    e2Wc = -e2.WindCnt;
                    break;
                default:
                    e2Wc = Math.Abs(e2.WindCnt);
                    break;
            }

            if (e1Contributing && e2Contributing)
            {
                if (e1Wc != 0 && e1Wc != 1 || e2Wc != 0 && e2Wc != 1 || e1.PolyTyp != e2.PolyTyp && m_ClipType != ClipType.ctXor)
                {
                    AddLocalMaxPoly(e1, e2, pt);
                }
                else
                {
                    AddOutPt(e1, pt);
                    AddOutPt(e2, pt);
                    SwapSides(e1, e2);
                    SwapPolyIndexes(e1, e2);
                }
            }
            else if (e1Contributing)
            {
                if (e2Wc == 0 || e2Wc == 1)
                {
                    AddOutPt(e1, pt);
                    SwapSides(e1, e2);
                    SwapPolyIndexes(e1, e2);
                }
            }
            else if (e2Contributing)
            {
                if (e1Wc == 0 || e1Wc == 1)
                {
                    AddOutPt(e2, pt);
                    SwapSides(e1, e2);
                    SwapPolyIndexes(e1, e2);
                }
            }
            else if ((e1Wc == 0 || e1Wc == 1) && (e2Wc == 0 || e2Wc == 1))
            {
                long e1Wc2, e2Wc2;
                switch (e1FillType2)
                {
                    case PolyFillType.pftPositive:
                        e1Wc2 = e1.WindCnt2;
                        break;
                    case PolyFillType.pftNegative:
                        e1Wc2 = -e1.WindCnt2;
                        break;
                    default:
                        e1Wc2 = Math.Abs(e1.WindCnt2);
                        break;
                }
                switch (e2FillType2)
                {
                    case PolyFillType.pftPositive:
                        e2Wc2 = e2.WindCnt2;
                        break;
                    case PolyFillType.pftNegative:
                        e2Wc2 = -e2.WindCnt2;
                        break;
                    default:
                        e2Wc2 = Math.Abs(e2.WindCnt2);
                        break;
                }

                if (e1.PolyTyp != e2.PolyTyp) AddLocalMinPoly(e1, e2, pt);
                else if (e1Wc == 1 && e2Wc == 1)
                {
                    switch (m_ClipType)
                    {
                        case ClipType.ctIntersection:
                            if (e1Wc2 > 0 && e2Wc2 > 0) AddLocalMinPoly(e1, e2, pt);
                            break;
                        case ClipType.ctUnion:
                            if (e1Wc2 <= 0 && e2Wc2 <= 0) AddLocalMinPoly(e1, e2, pt);
                            break;
                        case ClipType.ctDifference:
                            if (e1.PolyTyp == PolyType.ptClip && e1Wc2 > 0 && e2Wc2 > 0 ||
                                e1.PolyTyp == PolyType.ptSubject && e1Wc2 <= 0 && e2Wc2 <= 0)
                                AddLocalMinPoly(e1, e2, pt);
                            break;
                        case ClipType.ctXor:
                            AddLocalMinPoly(e1, e2, pt);
                            break;
                    }
                }
                else SwapSides(e1, e2);
            }
        }

        private void ProcessHorizontals()
        {
            TEdge horzEdge;
            while (PopEdgeFromSEL(out horzEdge)) ProcessHorizontal(horzEdge);
        }

        void GetHorzDirection(TEdge HorzEdge, out Direction Dir, out long Left, out long Right)
        {
            if (HorzEdge.Bot.x < HorzEdge.Top.x)
            {
                Left = HorzEdge.Bot.x;
                Right = HorzEdge.Top.x;
                Dir = Direction.dLeftToRight;
            }
            else
            {
                Left = HorzEdge.Top.x;
                Right = HorzEdge.Bot.x;
                Dir = Direction.dRightToLeft;
            }
        }

        private void ProcessHorizontal(TEdge horzEdge)
        {
            Direction dir;
            long horzLeft, horzRight;
            bool IsOpen = horzEdge.WindDelta == 0;

            GetHorzDirection(horzEdge, out dir, out horzLeft, out horzRight);

            TEdge eLastHorz = horzEdge, eMaxPair = null;
            while (eLastHorz.NextInLML != null && IsHorizontal(eLastHorz.NextInLML)) eLastHorz = eLastHorz.NextInLML;
            if (eLastHorz.NextInLML == null) eMaxPair = GetMaximaPair(eLastHorz);

            Maxima currMax = m_Maxima;
            if (currMax != null)
            {
                if (dir == Direction.dLeftToRight)
                {
                    while (currMax != null && currMax.X <= horzEdge.Bot.x) currMax = currMax.Next;
                    if (currMax != null && currMax.X >= eLastHorz.Top.x) currMax = null;
                }
                else
                {
                    while (currMax.Next != null && currMax.Next.X < horzEdge.Bot.x) currMax = currMax.Next;
                    if (currMax.X <= eLastHorz.Top.x) currMax = null;
                }
            }

            OutPt op1 = null;
            while (true)
            {
                bool IsLastHorz = horzEdge == eLastHorz;
                TEdge e = GetNextInAEL(horzEdge, dir);
                while (e != null)
                {
                    if (currMax != null)
                    {
                        if (dir == Direction.dLeftToRight)
                        {
                            while (currMax != null && currMax.X < e.Curr.x)
                            {
                                if (horzEdge.OutIdx >= 0 && !IsOpen) AddOutPt(horzEdge, new LPoint(currMax.X, horzEdge.Bot.y));
                                currMax = currMax.Next;
                            }
                        }
                        else
                        {
                            while (currMax != null && currMax.X > e.Curr.x)
                            {
                                if (horzEdge.OutIdx >= 0 && !IsOpen) AddOutPt(horzEdge, new LPoint(currMax.X, horzEdge.Bot.y));
                                currMax = currMax.Prev;
                            }
                        }
                    }

                    if (dir == Direction.dLeftToRight && e.Curr.x > horzRight || dir == Direction.dRightToLeft && e.Curr.x < horzLeft) break;
                    if (e.Curr.x == horzEdge.Top.x && horzEdge.NextInLML != null && e.Dx < horzEdge.NextInLML.Dx) break;

                    if (horzEdge.OutIdx >= 0 && !IsOpen)
                    {
                        op1 = AddOutPt(horzEdge, e.Curr);
                        TEdge eNextHorz = m_SortedEdges;
                        while (eNextHorz != null)
                        {
                            if (eNextHorz.OutIdx >= 0 && HorzSegmentsOverlap(horzEdge.Bot.x,
                              horzEdge.Top.x, eNextHorz.Bot.x, eNextHorz.Top.x))
                            {
                                OutPt op2 = GetLastOutPt(eNextHorz);
                                AddJoin(op2, op1, eNextHorz.Top);
                            }
                            eNextHorz = eNextHorz.NextInSEL;
                        }
                        AddGhostJoin(op1, horzEdge.Bot);
                    }

                    if (e == eMaxPair && IsLastHorz)
                    {
                        if (horzEdge.OutIdx >= 0) AddLocalMaxPoly(horzEdge, eMaxPair, horzEdge.Top);
                        DeleteFromAEL(horzEdge);
                        DeleteFromAEL(eMaxPair);
                        return;
                    }

                    if (dir == Direction.dLeftToRight)
                    {
                        LPoint Pt = new LPoint(e.Curr.x, horzEdge.Curr.y);
                        IntersectEdges(horzEdge, e, Pt);
                    }
                    else
                    {
                        LPoint Pt = new LPoint(e.Curr.x, horzEdge.Curr.y);
                        IntersectEdges(e, horzEdge, Pt);
                    }
                    TEdge eNext = GetNextInAEL(e, dir);
                    SwapPositionsInAEL(horzEdge, e);
                    e = eNext;
                }

                if (horzEdge.NextInLML == null || !IsHorizontal(horzEdge.NextInLML)) break;

                UpdateEdgeIntoAEL(ref horzEdge);
                if (horzEdge.OutIdx >= 0) AddOutPt(horzEdge, horzEdge.Bot);
                GetHorzDirection(horzEdge, out dir, out horzLeft, out horzRight);

            }

            if (horzEdge.OutIdx >= 0 && op1 == null)
            {
                op1 = GetLastOutPt(horzEdge);
                TEdge eNextHorz = m_SortedEdges;
                while (eNextHorz != null)
                {
                    if (eNextHorz.OutIdx >= 0 && HorzSegmentsOverlap(horzEdge.Bot.x,
                      horzEdge.Top.x, eNextHorz.Bot.x, eNextHorz.Top.x))
                    {
                        OutPt op2 = GetLastOutPt(eNextHorz);
                        AddJoin(op2, op1, eNextHorz.Top);
                    }
                    eNextHorz = eNextHorz.NextInSEL;
                }
                AddGhostJoin(op1, horzEdge.Top);
            }

            if (horzEdge.NextInLML != null)
            {
                if (horzEdge.OutIdx >= 0)
                {
                    op1 = AddOutPt(horzEdge, horzEdge.Top);

                    UpdateEdgeIntoAEL(ref horzEdge);
                    if (horzEdge.WindDelta == 0) return;
                    TEdge ePrev = horzEdge.PrevInAEL;
                    TEdge eNext = horzEdge.NextInAEL;
                    if (ePrev != null && ePrev.Curr.x == horzEdge.Bot.x &&
                       ePrev.Curr.y == horzEdge.Bot.y && ePrev.WindDelta != 0 && ePrev.OutIdx >= 0 &&
                       ePrev.Curr.y > ePrev.Top.y && SlopesEqual(horzEdge, ePrev))
                    {
                        OutPt op2 = AddOutPt(ePrev, horzEdge.Bot);
                        AddJoin(op1, op2, horzEdge.Top);
                    }
                    else if (eNext != null && eNext.Curr.x == horzEdge.Bot.x &&
                        eNext.Curr.y == horzEdge.Bot.y && eNext.WindDelta != 0 &&
                        eNext.OutIdx >= 0 && eNext.Curr.y > eNext.Top.y &&
                        SlopesEqual(horzEdge, eNext))
                    {
                        OutPt op2 = AddOutPt(eNext, horzEdge.Bot);
                        AddJoin(op1, op2, horzEdge.Top);
                    }
                }
                else UpdateEdgeIntoAEL(ref horzEdge);
            }
            else
            {
                if (horzEdge.OutIdx >= 0) AddOutPt(horzEdge, horzEdge.Top);
                DeleteFromAEL(horzEdge);
            }
        }

        private TEdge GetNextInAEL(TEdge e, Direction Direction)
        {
            return Direction == Direction.dLeftToRight ? e.NextInAEL : e.PrevInAEL;
        }

        private bool IsMaxima(TEdge e, double Y)
        {
            return e != null && e.Top.y == Y && e.NextInLML == null;
        }

        private bool IsIntermediate(TEdge e, double Y)
        {
            return e.Top.y == Y && e.NextInLML != null;
        }

        internal TEdge GetMaximaPair(TEdge e)
        {
            if (e.Next.Top == e.Top && e.Next.NextInLML == null) return e.Next;
            if (e.Prev.Top == e.Top && e.Prev.NextInLML == null) return e.Prev;
            return null;
        }

        internal TEdge GetMaximaPairEx(TEdge e)
        {
            TEdge result = GetMaximaPair(e);
            if (result == null || result.OutIdx == Skip || result.NextInAEL == result.PrevInAEL && !IsHorizontal(result)) return null;
            return result;
        }

        private bool ProcessIntersections(long topY)
        {
            if (m_ActiveEdges == null) return true;
            try
            {
                BuildIntersectList(topY);
                if (m_IntersectList.Count == 0) return true;
                if (m_IntersectList.Count == 1 || FixupIntersectionOrder())
                {
                    for (int i = 0; i < m_IntersectList.Count; i++)
                    {
                        IntersectNode iNode = m_IntersectList[i];
                        IntersectEdges(iNode.Edge1, iNode.Edge2, iNode.Pt);
                        SwapPositionsInAEL(iNode.Edge1, iNode.Edge2);
                    }
                    m_IntersectList.Clear();
                }
                else return false;
            }
            catch
            {
                m_SortedEdges = null;
                m_IntersectList.Clear();

            }
            m_SortedEdges = null;
            return true;
        }

        private void BuildIntersectList(long topY)
        {
            if (m_ActiveEdges == null) return;

            TEdge e = m_ActiveEdges;
            m_SortedEdges = e;
            while (e != null)
            {
                e.PrevInSEL = e.PrevInAEL;
                e.NextInSEL = e.NextInAEL;
                e.Curr.x = TopX(e, topY);
                e = e.NextInAEL;
            }

            bool isModified = true;
            while (isModified && m_SortedEdges != null)
            {
                isModified = false;
                e = m_SortedEdges;
                while (e.NextInSEL != null)
                {
                    TEdge eNext = e.NextInSEL;
                    if (e.Curr.x > eNext.Curr.x)
                    {
                        LPoint pt;
                        IntersectPoint(e, eNext, out pt);
                        if (pt.y < topY) pt = new LPoint(TopX(e, topY), topY);
                        m_IntersectList.Add(new IntersectNode
                        {
                            Edge1 = e,
                            Edge2 = eNext,
                            Pt = pt
                        });

                        SwapPositionsInSEL(e, eNext);
                        isModified = true;
                    }
                    else e = eNext;
                }
                if (e.PrevInSEL != null) e.PrevInSEL.NextInSEL = null;
                else break;
            }
            m_SortedEdges = null;
        }

        private bool EdgesAdjacent(IntersectNode inode)
        {
            return inode.Edge1.NextInSEL == inode.Edge2 || inode.Edge1.PrevInSEL == inode.Edge2;
        }

        private bool FixupIntersectionOrder()
        {
            m_IntersectList.Sort(delegate (IntersectNode n1, IntersectNode n2)
            {
                long i = n2.Pt.y - n1.Pt.y;
                if (i > 0) return 1;
                if (i < 0) return -1;
                return 0;
            });

            TEdge e = m_ActiveEdges;
            m_SortedEdges = e;
            while (e != null)
            {
                e.PrevInSEL = e.PrevInAEL;
                e.NextInSEL = e.NextInAEL;
                e = e.NextInAEL;
            }

            int cnt = m_IntersectList.Count;

            for (int i = 0; i < cnt; i++)
            {
                if (!EdgesAdjacent(m_IntersectList[i]))
                {
                    int j = i + 1;
                    while (j < cnt && !EdgesAdjacent(m_IntersectList[j])) j++;
                    if (j == cnt) return false;

                    IntersectNode tmp = m_IntersectList[i];
                    m_IntersectList[i] = m_IntersectList[j];
                    m_IntersectList[j] = tmp;

                }
                SwapPositionsInSEL(m_IntersectList[i].Edge1, m_IntersectList[i].Edge2);
            }
            return true;
        }

        internal static long Round(double value)
        {
            return value < 0 ? (long)(value - 0.5) : (long)(value + 0.5);
        }

        private static long TopX(TEdge edge, long currentY)
        {
            if (currentY == edge.Top.y) return edge.Top.x;
            return edge.Bot.x + Round(edge.Dx * (currentY - edge.Bot.y));
        }

        private void IntersectPoint(TEdge edge1, TEdge edge2, out LPoint ip)
        {
            ip = new LPoint();
            double b1, b2;

            if (edge1.Dx == edge2.Dx)
            {
                ip.y = edge1.Curr.y;
                ip.x = TopX(edge1, ip.y);
                return;
            }

            if (edge1.Delta.x == 0)
            {
                ip.x = edge1.Bot.x;
                if (IsHorizontal(edge2)) ip.y = edge2.Bot.y;
                else
                {
                    b2 = edge2.Bot.y - edge2.Bot.x / edge2.Dx;
                    ip.y = Round(ip.x / edge2.Dx + b2);
                }
            }
            else if (edge2.Delta.x == 0)
            {
                ip.x = edge2.Bot.x;
                if (IsHorizontal(edge1)) ip.y = edge1.Bot.y;
                else
                {
                    b1 = edge1.Bot.y - edge1.Bot.x / edge1.Dx;
                    ip.y = Round(ip.x / edge1.Dx + b1);
                }
            }
            else
            {
                b1 = edge1.Bot.x - edge1.Bot.y * edge1.Dx;
                b2 = edge2.Bot.x - edge2.Bot.y * edge2.Dx;
                double q = (b2 - b1) / (edge1.Dx - edge2.Dx);
                ip.y = Round(q);
                if (Math.Abs(edge1.Dx) < Math.Abs(edge2.Dx)) ip.x = Round(edge1.Dx * q + b1);
                else ip.x = Round(edge2.Dx * q + b2);
            }

            if (ip.y < edge1.Top.y || ip.y < edge2.Top.y)
            {
                if (edge1.Top.y > edge2.Top.y) ip.y = edge1.Top.y;
                else ip.y = edge2.Top.y;

                if (Math.Abs(edge1.Dx) < Math.Abs(edge2.Dx)) ip.x = TopX(edge1, ip.y);
                else ip.x = TopX(edge2, ip.y);
            }

            if (ip.y > edge1.Curr.y)
            {
                ip.y = edge1.Curr.y;
                if (Math.Abs(edge1.Dx) > Math.Abs(edge2.Dx)) ip.x = TopX(edge2, ip.y);
                else ip.x = TopX(edge1, ip.y);
            }
        }

        private void ProcessEdgesAtTopOfScanbeam(long topY)
        {
            TEdge e = m_ActiveEdges;
            while (e != null)
            {
                bool IsMaximaEdge = IsMaxima(e, topY);

                if (IsMaximaEdge)
                {
                    TEdge eMaxPair = GetMaximaPairEx(e);
                    IsMaximaEdge = eMaxPair == null || !IsHorizontal(eMaxPair);
                }

                if (IsMaximaEdge)
                {
                    if (StrictlySimple) InsertMaxima(e.Top.x);
                    TEdge ePrev = e.PrevInAEL;
                    DoMaxima(e);
                    e = ePrev == null ? m_ActiveEdges : ePrev.NextInAEL;
                }
                else
                {
                    if (IsIntermediate(e, topY) && IsHorizontal(e.NextInLML))
                    {
                        UpdateEdgeIntoAEL(ref e);
                        if (e.OutIdx >= 0) AddOutPt(e, e.Bot);
                        AddEdgeToSEL(e);
                    }
                    else
                    {
                        e.Curr.x = TopX(e, topY);
                        e.Curr.y = topY;
                    }

                    if (StrictlySimple)
                    {
                        TEdge ePrev = e.PrevInAEL;
                        if (e.OutIdx >= 0 && e.WindDelta != 0 && ePrev != null &&
                          ePrev.OutIdx >= 0 && ePrev.Curr.x == e.Curr.x &&
                          ePrev.WindDelta != 0)
                        {
                            LPoint ip = new LPoint(e.Curr);
                            OutPt op = AddOutPt(ePrev, ip);
                            OutPt op2 = AddOutPt(e, ip);
                            AddJoin(op, op2, ip);
                        }
                    }

                    e = e.NextInAEL;
                }
            }

            ProcessHorizontals();
            m_Maxima = null;

            e = m_ActiveEdges;
            while (e != null)
            {
                if (IsIntermediate(e, topY))
                {
                    OutPt op = null;
                    if (e.OutIdx >= 0) op = AddOutPt(e, e.Top);
                    UpdateEdgeIntoAEL(ref e);

                    TEdge ePrev = e.PrevInAEL;
                    TEdge eNext = e.NextInAEL;
                    if (ePrev != null && ePrev.Curr.x == e.Bot.x &&
                      ePrev.Curr.y == e.Bot.y && op != null &&
                      ePrev.OutIdx >= 0 && ePrev.Curr.y > ePrev.Top.y &&
                      SlopesEqual(e.Curr, e.Top, ePrev.Curr, ePrev.Top) &&
                      e.WindDelta != 0 && ePrev.WindDelta != 0)
                    {
                        OutPt op2 = AddOutPt(ePrev, e.Bot);
                        AddJoin(op, op2, e.Top);
                    }
                    else if (eNext != null && eNext.Curr.x == e.Bot.x &&
                        eNext.Curr.y == e.Bot.y && op != null &&
                        eNext.OutIdx >= 0 && eNext.Curr.y > eNext.Top.y &&
                        SlopesEqual(e.Curr, e.Top, eNext.Curr, eNext.Top) &&
                        e.WindDelta != 0 && eNext.WindDelta != 0)
                    {
                        OutPt op2 = AddOutPt(eNext, e.Bot);
                        AddJoin(op, op2, e.Top);
                    }
                }
                e = e.NextInAEL;
            }
        }

        private void DoMaxima(TEdge e)
        {
            TEdge eMaxPair = GetMaximaPairEx(e);
            if (eMaxPair == null)
            {
                if (e.OutIdx >= 0) AddOutPt(e, e.Top);
                DeleteFromAEL(e);
                return;
            }

            TEdge eNext = e.NextInAEL;
            while (eNext != null && eNext != eMaxPair)
            {
                IntersectEdges(e, eNext, e.Top);
                SwapPositionsInAEL(e, eNext);
                eNext = e.NextInAEL;
            }

            if (e.OutIdx == Unassigned && eMaxPair.OutIdx == Unassigned)
            {
                DeleteFromAEL(e);
                DeleteFromAEL(eMaxPair);
            }
            else if (e.OutIdx >= 0 && eMaxPair.OutIdx >= 0)
            {
                if (e.OutIdx >= 0) AddLocalMaxPoly(e, eMaxPair, e.Top);
                DeleteFromAEL(e);
                DeleteFromAEL(eMaxPair);
            }
            else if (e.WindDelta == 0)
            {
                if (e.OutIdx >= 0)
                {
                    AddOutPt(e, e.Top);
                    e.OutIdx = Unassigned;
                }
                DeleteFromAEL(e);

                if (eMaxPair.OutIdx >= 0)
                {
                    AddOutPt(eMaxPair, e.Top);
                    eMaxPair.OutIdx = Unassigned;
                }
                DeleteFromAEL(eMaxPair);
            }
        }

        private int PointCount(OutPt pts)
        {
            if (pts == null) return 0;
            int result = 0;
            OutPt p = pts;
            do
            {
                result++;
                p = p.Next;
            }
            while (p != pts);
            return result;
        }

        private void BuildResult(List<LPoints> polyg)
        {
            polyg.Clear();
            polyg.Capacity = m_PolyOuts.Count;
            for (int i = 0; i < m_PolyOuts.Count; i++)
            {
                OutRec outRec = m_PolyOuts[i];
                if (outRec.Pts == null) continue;
                OutPt p = outRec.Pts.Prev;
                int cnt = PointCount(p);
                if (cnt < 2) continue;
                LPoints pg = new LPoints(cnt);
                for (int j = 0; j < cnt; j++)
                {
                    pg.Add(p.Pt);
                    p = p.Prev;
                }
                polyg.Add(pg);
            }
        }

        private void BuildResult2(PolyTree polytree)
        {
            polytree.Clear();

            polytree.polygons.Capacity = m_PolyOuts.Count;
            for (int i = 0; i < m_PolyOuts.Count; i++)
            {
                OutRec outRec = m_PolyOuts[i];
                int cnt = PointCount(outRec.Pts);
                if (outRec.IsOpen && cnt < 2 || !outRec.IsOpen && cnt < 3) continue;
                FixHoleLinkage(outRec);
                PolyNode pn = new PolyNode();
                polytree.polygons.Add(pn);
                outRec.PolyNode = pn;
                pn.polygon.Capacity = cnt;
                OutPt op = outRec.Pts.Prev;
                for (int j = 0; j < cnt; j++)
                {
                    pn.polygon.Add(op.Pt);
                    op = op.Prev;
                }
            }

            polytree.children.Capacity = m_PolyOuts.Count;
            for (int i = 0; i < m_PolyOuts.Count; i++)
            {
                OutRec outRec = m_PolyOuts[i];
                if (outRec.PolyNode == null) continue;
                if (outRec.IsOpen)
                {
                    outRec.PolyNode.isOpen = true;
                    polytree.children.Add(outRec.PolyNode);
                }
                else if (outRec.FirstLeft != null && outRec.FirstLeft.PolyNode != null) outRec.FirstLeft.PolyNode.children.Add(outRec.PolyNode);
                else polytree.children.Add(outRec.PolyNode);
            }
        }

        private void FixupOutPolyline(OutRec outrec)
        {
            OutPt pp = outrec.Pts;
            OutPt lastPP = pp.Prev;
            while (pp != lastPP)
            {
                pp = pp.Next;
                if (pp.Pt == pp.Prev.Pt)
                {
                    if (pp == lastPP) lastPP = pp.Prev;
                    OutPt tmpPP = pp.Prev;
                    tmpPP.Next = pp.Next;
                    pp.Next.Prev = tmpPP;
                    pp = tmpPP;
                }
            }
            if (pp == pp.Prev) outrec.Pts = null;
        }

        private void FixupOutPolygon(OutRec outRec)
        {
            OutPt lastOK = null;
            outRec.BottomPt = null;
            OutPt pp = outRec.Pts;
            bool preserveCol = PreserveCollinear || StrictlySimple;
            while (true)
            {
                if (pp.Prev == pp || pp.Prev == pp.Next)
                {
                    outRec.Pts = null;
                    return;
                }

                if (pp.Pt == pp.Next.Pt || pp.Pt == pp.Prev.Pt ||
                  SlopesEqual(pp.Prev.Pt, pp.Pt, pp.Next.Pt) &&
                  (!preserveCol || !Pt2IsBetweenPt1AndPt3(pp.Prev.Pt, pp.Pt, pp.Next.Pt)))
                {
                    lastOK = null;
                    pp.Prev.Next = pp.Next;
                    pp.Next.Prev = pp.Prev;
                    pp = pp.Prev;
                }
                else if (pp == lastOK) break;
                else
                {
                    if (lastOK == null) lastOK = pp;
                    pp = pp.Next;
                }
            }
            outRec.Pts = pp;
        }

        OutPt DupOutPt(OutPt outPt, bool InsertAfter)
        {
            OutPt result = new OutPt
            {
                Pt = outPt.Pt,
                Idx = outPt.Idx
            };
            if (InsertAfter)
            {
                result.Next = outPt.Next;
                result.Prev = outPt;
                outPt.Next.Prev = result;
                outPt.Next = result;
            }
            else
            {
                result.Prev = outPt.Prev;
                result.Next = outPt;
                outPt.Prev.Next = result;
                outPt.Prev = result;
            }
            return result;
        }

        bool GetOverlap(long a1, long a2, long b1, long b2, out long Left, out long Right)
        {
            if (a1 < a2)
            {
                if (b1 < b2)
                {
                    Left = Math.Max(a1, b1);
                    Right = Math.Min(a2, b2);
                }
                else
                {
                    Left = Math.Max(a1, b2);
                    Right = Math.Min(a2, b1);
                }
            }
            else
            {
                if (b1 < b2)
                {
                    Left = Math.Max(a2, b1);
                    Right = Math.Min(a1, b2);
                }
                else
                {
                    Left = Math.Max(a2, b2);
                    Right = Math.Min(a1, b1);
                }
            }
            return Left < Right;
        }

        bool JoinHorz(OutPt op1, OutPt op1b, OutPt op2, OutPt op2b, LPoint Pt, bool DiscardLeft)
        {
            Direction Dir1 = op1.Pt.x > op1b.Pt.x ? Direction.dRightToLeft : Direction.dLeftToRight;
            Direction Dir2 = op2.Pt.x > op2b.Pt.x ? Direction.dRightToLeft : Direction.dLeftToRight;
            if (Dir1 == Dir2) return false;

            if (Dir1 == Direction.dLeftToRight)
            {
                while (op1.Next.Pt.x <= Pt.x && op1.Next.Pt.x >= op1.Pt.x && op1.Next.Pt.y == Pt.y) op1 = op1.Next;
                if (DiscardLeft && op1.Pt.x != Pt.x) op1 = op1.Next;
                op1b = DupOutPt(op1, !DiscardLeft);
                if (op1b.Pt != Pt)
                {
                    op1 = op1b;
                    op1.Pt = Pt;
                    op1b = DupOutPt(op1, !DiscardLeft);
                }
            }
            else
            {
                while (op1.Next.Pt.x >= Pt.x && op1.Next.Pt.x <= op1.Pt.x && op1.Next.Pt.y == Pt.y) op1 = op1.Next;
                if (!DiscardLeft && op1.Pt.x != Pt.x) op1 = op1.Next;
                op1b = DupOutPt(op1, DiscardLeft);
                if (op1b.Pt != Pt)
                {
                    op1 = op1b;
                    op1.Pt = Pt;
                    op1b = DupOutPt(op1, DiscardLeft);
                }
            }

            if (Dir2 == Direction.dLeftToRight)
            {
                while (op2.Next.Pt.x <= Pt.x && op2.Next.Pt.x >= op2.Pt.x && op2.Next.Pt.y == Pt.y) op2 = op2.Next;
                if (DiscardLeft && op2.Pt.x != Pt.x) op2 = op2.Next;
                op2b = DupOutPt(op2, !DiscardLeft);
                if (op2b.Pt != Pt)
                {
                    op2 = op2b;
                    op2.Pt = Pt;
                    op2b = DupOutPt(op2, !DiscardLeft);
                }
            }
            else
            {
                while (op2.Next.Pt.x >= Pt.x && op2.Next.Pt.x <= op2.Pt.x && op2.Next.Pt.y == Pt.y) op2 = op2.Next;
                if (!DiscardLeft && op2.Pt.x != Pt.x) op2 = op2.Next;
                op2b = DupOutPt(op2, DiscardLeft);
                if (op2b.Pt != Pt)
                {
                    op2 = op2b;
                    op2.Pt = Pt;
                    op2b = DupOutPt(op2, DiscardLeft);
                }
            }

            if (Dir1 == Direction.dLeftToRight == DiscardLeft)
            {
                op1.Prev = op2;
                op2.Next = op1;
                op1b.Next = op2b;
                op2b.Prev = op1b;
            }
            else
            {
                op1.Next = op2;
                op2.Prev = op1;
                op1b.Prev = op2b;
                op2b.Next = op1b;
            }
            return true;
        }

        private bool JoinPoints(Join j, OutRec outRec1, OutRec outRec2)
        {
            OutPt op1 = j.OutPt1, op1b;
            OutPt op2 = j.OutPt2, op2b;

            bool isHorizontal = j.OutPt1.Pt.y == j.OffPt.y;

            if (isHorizontal && j.OffPt == j.OutPt1.Pt && j.OffPt == j.OutPt2.Pt)
            {
                if (outRec1 != outRec2) return false;
                op1b = j.OutPt1.Next;
                while (op1b != op1 && op1b.Pt == j.OffPt) op1b = op1b.Next;
                bool reverse1 = op1b.Pt.y > j.OffPt.y;
                op2b = j.OutPt2.Next;
                while (op2b != op2 && op2b.Pt == j.OffPt) op2b = op2b.Next;
                bool reverse2 = op2b.Pt.y > j.OffPt.y;
                if (reverse1 == reverse2) return false;
                if (reverse1)
                {
                    op1b = DupOutPt(op1, false);
                    op2b = DupOutPt(op2, true);
                    op1.Prev = op2;
                    op2.Next = op1;
                    op1b.Next = op2b;
                    op2b.Prev = op1b;
                    j.OutPt1 = op1;
                    j.OutPt2 = op1b;
                    return true;
                }

                op1b = DupOutPt(op1, true);
                op2b = DupOutPt(op2, false);
                op1.Next = op2;
                op2.Prev = op1;
                op1b.Prev = op2b;
                op2b.Next = op1b;
                j.OutPt1 = op1;
                j.OutPt2 = op1b;
                return true;
            }
            if (isHorizontal)
            {
                op1b = op1;
                while (op1.Prev.Pt.y == op1.Pt.y && op1.Prev != op1b && op1.Prev != op2) op1 = op1.Prev;
                while (op1b.Next.Pt.y == op1b.Pt.y && op1b.Next != op1 && op1b.Next != op2) op1b = op1b.Next;
                if (op1b.Next == op1 || op1b.Next == op2) return false;

                op2b = op2;
                while (op2.Prev.Pt.y == op2.Pt.y && op2.Prev != op2b && op2.Prev != op1b) op2 = op2.Prev;
                while (op2b.Next.Pt.y == op2b.Pt.y && op2b.Next != op2 && op2b.Next != op1) op2b = op2b.Next;
                if (op2b.Next == op2 || op2b.Next == op1) return false;

                long Left, Right;
                if (!GetOverlap(op1.Pt.x, op1b.Pt.x, op2.Pt.x, op2b.Pt.x, out Left, out Right)) return false;

                LPoint Pt;
                bool DiscardLeftSide;
                if (op1.Pt.x >= Left && op1.Pt.x <= Right)
                {
                    Pt = op1.Pt;
                    DiscardLeftSide = op1.Pt.x > op1b.Pt.x;
                }
                else if (op2.Pt.x >= Left && op2.Pt.x <= Right)
                {
                    Pt = op2.Pt;
                    DiscardLeftSide = op2.Pt.x > op2b.Pt.x;
                }
                else if (op1b.Pt.x >= Left && op1b.Pt.x <= Right)
                {
                    Pt = op1b.Pt;
                    DiscardLeftSide = op1b.Pt.x > op1.Pt.x;
                }
                else
                {
                    Pt = op2b.Pt;
                    DiscardLeftSide = op2b.Pt.x > op2.Pt.x;
                }
                j.OutPt1 = op1;
                j.OutPt2 = op2;
                return JoinHorz(op1, op1b, op2, op2b, Pt, DiscardLeftSide);
            }

            op1b = op1.Next;
            while (op1b.Pt == op1.Pt && op1b != op1) op1b = op1b.Next;
            bool Reverse1 = op1b.Pt.y > op1.Pt.y || !SlopesEqual(op1.Pt, op1b.Pt, j.OffPt);
            if (Reverse1)
            {
                op1b = op1.Prev;
                while (op1b.Pt == op1.Pt && op1b != op1) op1b = op1b.Prev;
                if (op1b.Pt.y > op1.Pt.y || !SlopesEqual(op1.Pt, op1b.Pt, j.OffPt)) return false;
            }
            op2b = op2.Next;
            while (op2b.Pt == op2.Pt && op2b != op2) op2b = op2b.Next;
            bool Reverse2 = op2b.Pt.y > op2.Pt.y || !SlopesEqual(op2.Pt, op2b.Pt, j.OffPt);
            if (Reverse2)
            {
                op2b = op2.Prev;
                while (op2b.Pt == op2.Pt && op2b != op2) op2b = op2b.Prev;
                if (op2b.Pt.y > op2.Pt.y || !SlopesEqual(op2.Pt, op2b.Pt, j.OffPt)) return false;
            }

            if (op1b == op1 || op2b == op2 || op1b == op2b || outRec1 == outRec2 && Reverse1 == Reverse2) return false;

            if (Reverse1)
            {
                op1b = DupOutPt(op1, false);
                op2b = DupOutPt(op2, true);
                op1.Prev = op2;
                op2.Next = op1;
                op1b.Next = op2b;
                op2b.Prev = op1b;
                j.OutPt1 = op1;
                j.OutPt2 = op1b;
                return true;
            }

            op1b = DupOutPt(op1, true);
            op2b = DupOutPt(op2, false);
            op1.Next = op2;
            op2.Prev = op1;
            op1b.Prev = op2b;
            op2b.Next = op1b;
            j.OutPt1 = op1;
            j.OutPt2 = op1b;
            return true;
        }

        private static int PointInPolygon(LPoint pt, OutPt op)
        {
            int result = 0;
            OutPt startOp = op;
            long ptx = pt.x, pty = pt.y;
            long poly0x = op.Pt.x, poly0y = op.Pt.y;
            do
            {
                op = op.Next;
                long poly1x = op.Pt.x, poly1y = op.Pt.y;

                if (poly1y == pty)
                {
                    if (poly1x == ptx || poly0y == pty && poly1x > ptx == poly0x < ptx) return -1;
                }
                if (poly0y < pty != poly1y < pty)
                {
                    if (poly0x >= ptx)
                    {
                        if (poly1x > ptx) result = 1 - result;
                        else
                        {
                            double d = (double)(poly0x - ptx) * (poly1y - pty) - (double)(poly1x - ptx) * (poly0y - pty);
                            if (d == 0) return -1;
                            if (d > 0 == poly1y > poly0y) result = 1 - result;
                        }
                    }
                    else if (poly1x > ptx)
                    {
                        double d = (double)(poly0x - ptx) * (poly1y - pty) - (double)(poly1x - ptx) * (poly0y - pty);
                        if (d == 0) return -1;
                        if (d > 0 == poly1y > poly0y) result = 1 - result;
                    }
                }
                poly0x = poly1x;
                poly0y = poly1y;
            }
            while (startOp != op);
            return result;
        }

        private static bool Poly2ContainsPoly1(OutPt outPt1, OutPt outPt2)
        {
            OutPt op = outPt1;
            do
            {
                int res = PointInPolygon(op.Pt, outPt2);
                if (res >= 0) return res > 0;
                op = op.Next;
            }
            while (op != outPt1);
            return true;
        }

        private void FixupFirstLefts1(OutRec OldOutRec, OutRec NewOutRec)
        {
            foreach (OutRec outRec in m_PolyOuts)
            {
                OutRec firstLeft = ParseFirstLeft(outRec.FirstLeft);
                if (outRec.Pts != null && firstLeft == OldOutRec)
                {
                    if (Poly2ContainsPoly1(outRec.Pts, NewOutRec.Pts)) outRec.FirstLeft = NewOutRec;
                }
            }
        }

        private void FixupFirstLefts2(OutRec innerOutRec, OutRec outerOutRec)
        {
            OutRec orfl = outerOutRec.FirstLeft;
            foreach (OutRec outRec in m_PolyOuts)
            {
                if (outRec.Pts == null || outRec == outerOutRec || outRec == innerOutRec) continue;
                OutRec firstLeft = ParseFirstLeft(outRec.FirstLeft);
                if (firstLeft != orfl && firstLeft != innerOutRec && firstLeft != outerOutRec) continue;
                if (Poly2ContainsPoly1(outRec.Pts, innerOutRec.Pts)) outRec.FirstLeft = innerOutRec;
                else if (Poly2ContainsPoly1(outRec.Pts, outerOutRec.Pts)) outRec.FirstLeft = outerOutRec;
                else if (outRec.FirstLeft == innerOutRec || outRec.FirstLeft == outerOutRec) outRec.FirstLeft = orfl;
            }
        }

        private void FixupFirstLefts3(OutRec OldOutRec, OutRec NewOutRec)
        {
            foreach (OutRec outRec in m_PolyOuts)
            {
                ParseFirstLeft(outRec.FirstLeft);
                if (outRec.Pts != null && outRec.FirstLeft == OldOutRec) outRec.FirstLeft = NewOutRec;
            }
        }

        private static OutRec ParseFirstLeft(OutRec FirstLeft)
        {
            while (FirstLeft != null && FirstLeft.Pts == null) FirstLeft = FirstLeft.FirstLeft;
            return FirstLeft;
        }

        private void JoinCommonEdges()
        {
            for (int i = 0; i < m_Joins.Count; i++)
            {
                Join join = m_Joins[i];

                OutRec outRec1 = GetOutRec(join.OutPt1.Idx);
                OutRec outRec2 = GetOutRec(join.OutPt2.Idx);

                if (outRec1.Pts == null || outRec2.Pts == null) continue;
                if (outRec1.IsOpen || outRec2.IsOpen) continue;

                OutRec holeStateRec;
                if (outRec1 == outRec2) holeStateRec = outRec1;
                else if (OutRec1RightOfOutRec2(outRec1, outRec2)) holeStateRec = outRec2;
                else if (OutRec1RightOfOutRec2(outRec2, outRec1)) holeStateRec = outRec1;
                else holeStateRec = GetLowermostRec(outRec1, outRec2);

                if (!JoinPoints(join, outRec1, outRec2)) continue;

                if (outRec1 == outRec2)
                {
                    outRec1.Pts = join.OutPt1;
                    outRec1.BottomPt = null;
                    outRec2 = CreateOutRec();
                    outRec2.Pts = join.OutPt2;

                    UpdateOutPtIdxs(outRec2);

                    if (Poly2ContainsPoly1(outRec2.Pts, outRec1.Pts))
                    {
                        outRec2.IsHole = !outRec1.IsHole;
                        outRec2.FirstLeft = outRec1;

                        if (m_UsingPolyTree) FixupFirstLefts2(outRec2, outRec1);
                        if ((outRec2.IsHole ^ ReverseSolution) == Area(outRec2) > 0) ReversePolyPtLinks(outRec2.Pts);

                    }
                    else if (Poly2ContainsPoly1(outRec1.Pts, outRec2.Pts))
                    {
                        outRec2.IsHole = outRec1.IsHole;
                        outRec1.IsHole = !outRec2.IsHole;
                        outRec2.FirstLeft = outRec1.FirstLeft;
                        outRec1.FirstLeft = outRec2;

                        if (m_UsingPolyTree) FixupFirstLefts2(outRec1, outRec2);
                        if ((outRec1.IsHole ^ ReverseSolution) == Area(outRec1) > 0) ReversePolyPtLinks(outRec1.Pts);
                    }
                    else
                    {
                        outRec2.IsHole = outRec1.IsHole;
                        outRec2.FirstLeft = outRec1.FirstLeft;

                        if (m_UsingPolyTree) FixupFirstLefts1(outRec1, outRec2);
                    }

                }
                else
                {
                    outRec2.Pts = null;
                    outRec2.BottomPt = null;
                    outRec2.Idx = outRec1.Idx;

                    outRec1.IsHole = holeStateRec.IsHole;
                    if (holeStateRec == outRec2) outRec1.FirstLeft = outRec2.FirstLeft;
                    outRec2.FirstLeft = outRec1;

                    if (m_UsingPolyTree) FixupFirstLefts3(outRec2, outRec1);
                }
            }
        }

        private void UpdateOutPtIdxs(OutRec outrec)
        {
            OutPt op = outrec.Pts;
            do
            {
                op.Idx = outrec.Idx;
                op = op.Prev;
            }
            while (op != outrec.Pts);
        }

        private void DoSimplePolygons()
        {
            int i = 0;
            while (i < m_PolyOuts.Count)
            {
                OutRec outrec = m_PolyOuts[i++];
                OutPt op = outrec.Pts;
                if (op == null || outrec.IsOpen) continue;
                do
                {
                    OutPt op2 = op.Next;
                    while (op2 != outrec.Pts)
                    {
                        if (op.Pt == op2.Pt && op2.Next != op && op2.Prev != op)
                        {
                            OutPt op3 = op.Prev;
                            OutPt op4 = op2.Prev;
                            op.Prev = op4;
                            op4.Next = op;
                            op2.Prev = op3;
                            op3.Next = op2;

                            outrec.Pts = op;
                            OutRec outrec2 = CreateOutRec();
                            outrec2.Pts = op2;
                            UpdateOutPtIdxs(outrec2);
                            if (Poly2ContainsPoly1(outrec2.Pts, outrec.Pts))
                            {
                                outrec2.IsHole = !outrec.IsHole;
                                outrec2.FirstLeft = outrec;
                                if (m_UsingPolyTree) FixupFirstLefts2(outrec2, outrec);
                            }
                            else if (Poly2ContainsPoly1(outrec.Pts, outrec2.Pts))
                            {
                                outrec2.IsHole = outrec.IsHole;
                                outrec.IsHole = !outrec2.IsHole;
                                outrec2.FirstLeft = outrec.FirstLeft;
                                outrec.FirstLeft = outrec2;
                                if (m_UsingPolyTree) FixupFirstLefts2(outrec, outrec2);
                            }
                            else
                            {
                                outrec2.IsHole = outrec.IsHole;
                                outrec2.FirstLeft = outrec.FirstLeft;
                                if (m_UsingPolyTree) FixupFirstLefts1(outrec, outrec2);
                            }
                            op2 = op;
                        }
                        op2 = op2.Next;
                    }
                    op = op.Next;
                }
                while (op != outrec.Pts);
            }
        }

        internal double Area(OutRec outRec)
        {
            return Area(outRec.Pts);
        }

        internal double Area(OutPt op)
        {
            OutPt opFirst = op;
            if (op == null) return 0;
            double a = 0;
            do
            {
                a = a + (op.Prev.Pt.x + op.Pt.x) * (double)(op.Prev.Pt.y - op.Pt.y);
                op = op.Next;
            }
            while (op != opFirst);
            return a * 0.5;
        }

        internal enum NodeType { ntAny, ntOpen, ntClosed }

        public static List<LPoints> PolyTreeToPaths(PolyTree polytree)
        {
            List<LPoints> result = new List<LPoints>( polytree.Total );
            AddPolyNodeToPaths(polytree, NodeType.ntAny, result);
            return result;
        }

        internal static void AddPolyNodeToPaths(PolyNode polynode, NodeType nt, List<LPoints> paths)
        {
            bool match = true;
            switch (nt)
            {
                case NodeType.ntOpen:
                    return;
                case NodeType.ntClosed:
                    match = !polynode.isOpen;
                    break;
            }

            if (polynode.polygon.Count > 0 && match) paths.Add(polynode.polygon);
            foreach (PolyNode pn in polynode.children) AddPolyNodeToPaths(pn, nt, paths);
        }
    }
}