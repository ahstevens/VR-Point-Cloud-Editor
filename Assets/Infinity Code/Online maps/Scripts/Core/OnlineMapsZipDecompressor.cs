/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.IO;

/// <summary>
/// Implements decompression of Zip archives
/// </summary>
public static class OnlineMapsZipDecompressor
{
    /// <summary>
    /// Decompression zip 
    /// </summary>
    /// <param name="bytes">Zip archive bytes</param>
    /// <returns>Uncompressed bytes</returns>
    public static byte[] Decompress(byte[] bytes)
    {
        if (bytes.Length < 2 || bytes[0] != 0x1f || bytes[1] != 0x8b) return bytes;

        InputStream stream = new InputStream(bytes);
        const int SIZE = 4096;
        byte[] buffer = new byte[SIZE];
        MemoryStream memory = new MemoryStream();

        try
        {
            int count;
            do
            {
                count = stream.Read(buffer, 0, SIZE);
                if (count > 0) memory.Write(buffer, 0, count);
            } while (count > 0);
        }
        catch 
        {
            return bytes;
        }

        return memory.ToArray();
    }

    internal class Buffer
    {
        internal int available;
        private byte[] clearText;
        private int clearTextLength;
        private Stream inputStream;
        private byte[] rawData;
        private int rawLength;

        internal Buffer(Stream stream, int bufferSize)
        {
            inputStream = stream;
            if (bufferSize < 1024) bufferSize = 1024;
            rawData = new byte[bufferSize];
            clearText = rawData;
        }

        internal void Fill()
        {
            rawLength = 0;
            int toRead = rawData.Length;

            while (toRead > 0)
            {
                int count = inputStream.Read(rawData, rawLength, toRead);
                if (count <= 0) break;
                rawLength += count;
                toRead -= count;
            }

            clearTextLength = rawLength;
            available = clearTextLength;
        }

        internal int ReadClearTextBuffer(byte[] outBuffer, int offset, int length)
        {
            int currentOffset = offset;
            int currentLength = length;

            while (currentLength > 0)
            {
                if (available <= 0)
                {
                    Fill();
                    if (available <= 0) return 0;
                }

                int toCopy = Math.Min(currentLength, available);
                Array.Copy(clearText, clearTextLength - available, outBuffer, currentOffset, toCopy);
                currentOffset += toCopy;
                currentLength -= toCopy;
                available -= toCopy;
            }
            return length;
        }

        internal int ReadLeByte()
        {
            if (available <= 0) Fill();
            byte result = rawData[rawLength - available];
            available -= 1;
            return result;
        }

        internal void SetInflaterInput(Inflater inflater)
        {
            if (available <= 0) return;

            inflater.input.SetInput(clearText, clearTextLength - available, available);
            available = 0;
        }
    }

    internal class Crc32
    {
        private static readonly uint[] crcTable = {
            0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419,
            0x706AF48F, 0xE963A535, 0x9E6495A3, 0x0EDB8832, 0x79DCB8A4,
            0xE0D5E91E, 0x97D2D988, 0x09B64C2B, 0x7EB17CBD, 0xE7B82D07,
            0x90BF1D91, 0x1DB71064, 0x6AB020F2, 0xF3B97148, 0x84BE41DE,
            0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7, 0x136C9856,
            0x646BA8C0, 0xFD62F97A, 0x8A65C9EC, 0x14015C4F, 0x63066CD9,
            0xFA0F3D63, 0x8D080DF5, 0x3B6E20C8, 0x4C69105E, 0xD56041E4,
            0xA2677172, 0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B,
            0x35B5A8FA, 0x42B2986C, 0xDBBBC9D6, 0xACBCF940, 0x32D86CE3,
            0x45DF5C75, 0xDCD60DCF, 0xABD13D59, 0x26D930AC, 0x51DE003A,
            0xC8D75180, 0xBFD06116, 0x21B4F4B5, 0x56B3C423, 0xCFBA9599,
            0xB8BDA50F, 0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924,
            0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D, 0x76DC4190,
            0x01DB7106, 0x98D220BC, 0xEFD5102A, 0x71B18589, 0x06B6B51F,
            0x9FBFE4A5, 0xE8B8D433, 0x7807C9A2, 0x0F00F934, 0x9609A88E,
            0xE10E9818, 0x7F6A0DBB, 0x086D3D2D, 0x91646C97, 0xE6635C01,
            0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E, 0x6C0695ED,
            0x1B01A57B, 0x8208F4C1, 0xF50FC457, 0x65B0D9C6, 0x12B7E950,
            0x8BBEB8EA, 0xFCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3,
            0xFBD44C65, 0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2,
            0x4ADFA541, 0x3DD895D7, 0xA4D1C46D, 0xD3D6F4FB, 0x4369E96A,
            0x346ED9FC, 0xAD678846, 0xDA60B8D0, 0x44042D73, 0x33031DE5,
            0xAA0A4C5F, 0xDD0D7CC9, 0x5005713C, 0x270241AA, 0xBE0B1010,
            0xC90C2086, 0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F,
            0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 0x59B33D17,
            0x2EB40D81, 0xB7BD5C3B, 0xC0BA6CAD, 0xEDB88320, 0x9ABFB3B6,
            0x03B6E20C, 0x74B1D29A, 0xEAD54739, 0x9DD277AF, 0x04DB2615,
            0x73DC1683, 0xE3630B12, 0x94643B84, 0x0D6D6A3E, 0x7A6A5AA8,
            0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1, 0xF00F9344,
            0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0xF762575D, 0x806567CB,
            0x196C3671, 0x6E6B06E7, 0xFED41B76, 0x89D32BE0, 0x10DA7A5A,
            0x67DD4ACC, 0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5,
            0xD6D6A3E8, 0xA1D1937E, 0x38D8C2C4, 0x4FDFF252, 0xD1BB67F1,
            0xA6BC5767, 0x3FB506DD, 0x48B2364B, 0xD80D2BDA, 0xAF0A1B4C,
            0x36034AF6, 0x41047A60, 0xDF60EFC3, 0xA867DF55, 0x316E8EEF,
            0x4669BE79, 0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236,
            0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F, 0xC5BA3BBE,
            0xB2BD0B28, 0x2BB45A92, 0x5CB36A04, 0xC2D7FFA7, 0xB5D0CF31,
            0x2CD99E8B, 0x5BDEAE1D, 0x9B64C2B0, 0xEC63F226, 0x756AA39C,
            0x026D930A, 0x9C0906A9, 0xEB0E363F, 0x72076785, 0x05005713,
            0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38, 0x92D28E9B,
            0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21, 0x86D3D2D4, 0xF1D4E242,
            0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0xF6B9265B, 0x6FB077E1,
            0x18B74777, 0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C,
            0x8F659EFF, 0xF862AE69, 0x616BFFD3, 0x166CCF45, 0xA00AE278,
            0xD70DD2EE, 0x4E048354, 0x3903B3C2, 0xA7672661, 0xD06016F7,
            0x4969474D, 0x3E6E77DB, 0xAED16A4A, 0xD9D65ADC, 0x40DF0B66,
            0x37D83BF0, 0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9,
            0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6, 0xBAD03605,
            0xCDD70693, 0x54DE5729, 0x23D967BF, 0xB3667A2E, 0xC4614AB8,
            0x5D681B02, 0x2A6F2B94, 0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B,
            0x2D02EF8D
        };

        private uint checkValue;

        internal Crc32()
        {
            checkValue = 0xFFFFFFFF;
        }

        internal void Update(int bval)
        {
            checkValue = crcTable[(checkValue ^ bval) & 0xFF] ^ (checkValue >> 8);
        }
    }

    internal class Header
    {
        private static readonly int[] BL_ORDER = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
        private static readonly int[] repBits = { 2, 3, 7 };
        private static readonly int[] repMin = { 3, 3, 11 };

        private byte[] blLens;
        private HuffmanTree blTree;
        private int blnum;
        private int dnum;
        private byte lastLen;
        private byte[] litdistLens;
        private int lnum;
        private int mode;
        private int num;
        private int ptr;
        private int repSymbol;

        internal HuffmanTree BuildDistTree()
        {
            byte[] distLens = new byte[dnum];
            Array.Copy(litdistLens, lnum, distLens, 0, dnum);
            return new HuffmanTree(distLens);
        }

        internal HuffmanTree BuildLitLenTree()
        {
            byte[] litlenLens = new byte[lnum];
            Array.Copy(litdistLens, 0, litlenLens, 0, lnum);
            return new HuffmanTree(litlenLens);
        }

        internal bool Decode(StreamManipulator input)
        {
            decode_loop:
            while (true)
            {
                switch (mode)
                {
                    case 0:
                        lnum = input.PeekBits(5);
                        if (lnum < 0) return false;
                        lnum += 257;
                        input.DropBits(5);
                        mode = 1;
                        goto case 1;
                    case 1:
                        dnum = input.PeekBits(5);
                        if (dnum < 0) return false;
                        dnum++;
                        input.DropBits(5);
                        num = lnum + dnum;
                        litdistLens = new byte[num];
                        mode = 2;
                        goto case 2;
                    case 2:
                        blnum = input.PeekBits(4);
                        if (blnum < 0) return false;
                        blnum += 4;
                        input.DropBits(4);
                        blLens = new byte[19];
                        ptr = 0;
                        mode = 3;
                        goto case 3;
                    case 3:
                        while (ptr < blnum)
                        {
                            int len = input.PeekBits(3);
                            if (len < 0) return false;
                            input.DropBits(3);
                            blLens[BL_ORDER[ptr]] = (byte)len;
                            ptr++;
                        }
                        blTree = new HuffmanTree(blLens);
                        blLens = null;
                        ptr = 0;
                        mode = 4;
                        goto case 4;
                    case 4:
                        int symbol;
                        while (((symbol = blTree.GetSymbol(input)) & ~15) == 0)
                        {
                            litdistLens[ptr++] = lastLen = (byte)symbol;
                            if (ptr == num) return true;
                        }

                        if (symbol < 0) return false;

                        if (symbol >= 17) lastLen = 0;
                        repSymbol = symbol - 16;
                        mode = 5;
                        goto case 5;
                    case 5:
                        int bits = repBits[repSymbol];
                        int count = input.PeekBits(bits);
                        if (count < 0) return false;
                        input.DropBits(bits);
                        count += repMin[repSymbol];
                        while (count-- > 0) litdistLens[ptr++] = lastLen;

                        if (ptr == num) return true;
                        mode = 4;
                        goto decode_loop;
                }
            }
        }
    }

    internal class HuffmanTree
    {
        private const int MAX_BITLEN = 15;
        private static readonly byte[] bit4Reverse = { 0, 8, 4, 12, 2, 10, 6, 14, 1, 9, 5, 13, 3, 11, 7, 15 };

        private short[] tree;

        internal HuffmanTree(byte[] codeLengths)
        {
            int[] blCount = new int[MAX_BITLEN + 1];
            int[] nextCode = new int[MAX_BITLEN + 1];

            for (int i = 0; i < codeLengths.Length; i++)
            {
                int bits = codeLengths[i];
                if (bits > 0) blCount[bits]++;
            }

            int code = 0;
            int treeSize = 512;
            for (int bits = 1; bits <= MAX_BITLEN; bits++)
            {
                nextCode[bits] = code;
                code += blCount[bits] << (16 - bits);
                if (bits >= 10)
                {
                    int start = nextCode[bits] & 0x1ff80;
                    int end = code & 0x1ff80;
                    treeSize += (end - start) >> (16 - bits);
                }
            }

            tree = new short[treeSize];
            int treePtr = 512;
            for (int bits = MAX_BITLEN; bits >= 10; bits--)
            {
                int end = code & 0x1ff80;
                code -= blCount[bits] << (16 - bits);
                int start = code & 0x1ff80;
                for (int i = start; i < end; i += 1 << 7)
                {
                    tree[BitReverse(i)] = (short)((-treePtr << 4) | bits);
                    treePtr += 1 << (bits - 9);
                }
            }

            for (int i = 0; i < codeLengths.Length; i++)
            {
                int bits = codeLengths[i];
                if (bits == 0) continue;
                code = nextCode[bits];
                int revcode = BitReverse(code);
                if (bits <= 9)
                {
                    do
                    {
                        tree[revcode] = (short)((i << 4) | bits);
                        revcode += 1 << bits;
                    } while (revcode < 512);
                }
                else
                {
                    int subTree = tree[revcode & 511];
                    int treeLen = 1 << (subTree & 15);
                    subTree = -(subTree >> 4);
                    do
                    {
                        tree[subTree | (revcode >> 9)] = (short)((i << 4) | bits);
                        revcode += 1 << bits;
                    } while (revcode < treeLen);
                }
                nextCode[bits] = code + (1 << (16 - bits));
            }
        }

        internal static short BitReverse(int toReverse)
        {
            return (short)(bit4Reverse[toReverse & 0xF] << 12 |
                           bit4Reverse[(toReverse >> 4) & 0xF] << 8 |
                           bit4Reverse[(toReverse >> 8) & 0xF] << 4 |
                           bit4Reverse[toReverse >> 12]);
        }

        internal int GetSymbol(StreamManipulator input)
        {
            int lookahead, symbol;
            if ((lookahead = input.PeekBits(9)) >= 0)
            {
                if ((symbol = tree[lookahead]) >= 0)
                {
                    input.DropBits(symbol & 15);
                    return symbol >> 4;
                }
                int subtree = -(symbol >> 4);
                int bitlen = symbol & 15;
                if ((lookahead = input.PeekBits(bitlen)) >= 0)
                {
                    symbol = tree[subtree | (lookahead >> 9)];
                    input.DropBits(symbol & 15);
                    return symbol >> 4;
                }
                int bits = input.availableBits;
                lookahead = input.PeekBits(bits);
                symbol = tree[subtree | (lookahead >> 9)];
                if ((symbol & 15) <= bits)
                {
                    input.DropBits(symbol & 15);
                    return symbol >> 4;
                }
                return -1;
            }
            else
            {
                int bits = input.availableBits;
                lookahead = input.PeekBits(bits);
                symbol = tree[lookahead];
                if (symbol >= 0 && (symbol & 15) <= bits)
                {
                    input.DropBits(symbol & 15);
                    return symbol >> 4;
                }
                return -1;
            }
        }
    }

    internal class Inflater
    {
        private static readonly int[] CPDEXT = {
            0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
            7, 7, 8, 8, 9, 9, 10, 10, 11, 11,
            12, 12, 13, 13
        };

        private static readonly int[] CPDIST = {
            1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
            257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145,
            8193, 12289, 16385, 24577
        };

        private static readonly int[] CPLENS = {
            3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
            35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258
        };

        private static readonly int[] CPLEXT = {
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
            3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0
        };

        internal readonly StreamManipulator input;

        private uint adler = 1;
        private HuffmanTree distTree;
        private Header dynHeader;
        private bool isLastBlock;
        private HuffmanTree litlenTree;
        private int mode;
        private int neededBits;
        private Output output;
        private int readAdler;
        private int repDist;
        private int repLength;
        private int uncomprLen;

        internal bool IsFinished
        {
            get { return mode == 12 && output.available == 0; }
        }

        internal Inflater()
        {
            input = new StreamManipulator();
            output = new Output();
            mode = 2;
        }

        private bool Decode()
        {
            switch (mode)
            {
                case 0:
                    return DecodeHeader();

                case 1:
                    return DecodeDict();

                case 11:
                    return DecodeChecksum();

                case 2:
                    if (isLastBlock)
                    {
                        mode = 12;
                        return false;
                    }

                    int type = input.PeekBits(3);
                    if (type < 0) return false;
                    input.DropBits(3);

                    isLastBlock |= (type & 1) != 0;
                    switch (type >> 1)
                    {
                        case 0:
                            input.SkipToByteBoundary();
                            mode = 3;
                            break;
                        case 1:
                            litlenTree = null;
                            distTree = null;
                            mode = 7;
                            break;
                        case 2:
                            dynHeader = new Header();
                            mode = 6;
                            break;
                    }
                    return true;

                case 3:
                {
                    if ((uncomprLen = input.PeekBits(16)) < 0) return false;
                    input.DropBits(16);
                    mode = 4;
                }
                    goto case 4;

                case 4:
                {
                    int nlen = input.PeekBits(16);
                    if (nlen < 0) return false;
                    input.DropBits(16);
                    mode = 5;
                }
                    goto case 5;

                case 5:
                {
                    int more = output.CopyStored(input, uncomprLen);
                    uncomprLen -= more;
                    if (uncomprLen == 0)
                    {
                        mode = 2;
                        return true;
                    }
                    return !input.isNeedingInput;
                }

                case 6:
                    if (!dynHeader.Decode(input)) return false;

                    litlenTree = dynHeader.BuildLitLenTree();
                    distTree = dynHeader.BuildDistTree();
                    mode = 7;
                    goto case 7;

                case 7:
                case 8:
                case 9:
                case 10:
                    return DecodeHuffman();

                case 12:
                    return false;

                default:
                    throw new Exception("Inflater.Decode unknown mode");
            }
        }

        private bool DecodeChecksum()
        {
            while (neededBits > 0)
            {
                int chkByte = input.PeekBits(8);
                if (chkByte < 0) return false;
                input.DropBits(8);
                readAdler = (readAdler << 8) | chkByte;
                neededBits -= 8;
            }

            mode = 12;
            return false;
        }

        private bool DecodeDict()
        {
            while (neededBits > 0)
            {
                int dictByte = input.PeekBits(8);
                if (dictByte < 0) return false;
                input.DropBits(8);
                readAdler = (readAdler << 8) | dictByte;
                neededBits -= 8;
            }
            return false;
        }

        private bool DecodeHeader()
        {
            int header = input.PeekBits(16);
            if (header < 0) return false;
            input.DropBits(16);

            header = ((header << 8) | (header >> 8)) & 0xffff;

            if ((header & 0x0020) == 0) mode = 2;
            else
            {
                mode = 1;
                neededBits = 32;
            }
            return true;
        }

        private bool DecodeHuffman()
        {
            int free = output.freeSpace;
            while (free >= 258)
            {
                int symbol;
                switch (mode)
                {
                    case 7:
                        while (((symbol = litlenTree.GetSymbol(input)) & ~0xff) == 0)
                        {
                            output.Write(symbol);
                            if (--free < 258) return true;
                        }

                        if (symbol < 257)
                        {
                            if (symbol < 0) return false;
                            distTree = null;
                            litlenTree = null;
                            mode = 2;
                            return true;
                        }

                        int s = symbol - 257;
                        if (s < 29)
                        {
                            repLength = CPLENS[s];
                            neededBits = CPLEXT[s];
                        }

                        goto case 8;

                    case 8:
                        if (neededBits > 0)
                        {
                            mode = 8;
                            int i = input.PeekBits(neededBits);
                            if (i < 0) return false;
                            input.DropBits(neededBits);
                            repLength += i;
                        }
                        mode = 9;
                        goto case 9;

                    case 9:
                        symbol = distTree.GetSymbol(input);
                        if (symbol < 0) return false;

                        if (symbol < 30)
                        {
                            repDist = CPDIST[symbol];
                            neededBits = CPDEXT[symbol];
                        }

                        goto case 10;

                    case 10:
                        if (neededBits > 0)
                        {
                            mode = 10;
                            int i = input.PeekBits(neededBits);
                            if (i < 0) return false;
                            input.DropBits(neededBits);
                            repDist += i;
                        }

                        output.Repeat(repLength, repDist);
                        free -= repLength;
                        mode = 7;
                        break;
                }
            }
            return true;
        }

        internal int Inflate(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                if (!IsFinished) Decode();
                return 0;
            }

            int bytesCopied = 0;

            do
            {
                if (mode == 11) continue;

                int more = output.CopyOutput(buffer, offset, count);
                if (more <= 0) continue;

                UpdateAdler(buffer, offset, more);
                offset += more;
                bytesCopied += more;
                count -= more;
                if (count == 0) return bytesCopied;
            } while (Decode() || output.available > 0 && mode != 11);
            return bytesCopied;
        }

        internal void Reset()
        {
            mode = 2;
            input.Reset();
            output.Reset();
            dynHeader = null;
            litlenTree = null;
            distTree = null;
            isLastBlock = false;
            adler = 1;
        }

        internal void UpdateAdler(byte[] buffer, int offset, int count)
        {
            uint s1 = adler & 0xFFFF;
            uint s2 = adler >> 16;

            while (count > 0)
            {
                int n = 3800;
                if (n > count) n = count;
                count -= n;
                while (--n >= 0)
                {
                    s1 = s1 + (uint)(buffer[offset++] & 0xff);
                    s2 = s2 + s1;
                }
                s1 %= 65521;
                s2 %= 65521;
            }

            adler = (s2 << 16) | s1;
        }
    }

    internal class InputStream : IDisposable
    {
        private Buffer buffer;
        private Crc32 crc;
        private Inflater inflater;
        private bool readHeader;
        private Stream stream;

        internal InputStream(byte[] bytes)
        {
            stream = new MemoryStream(bytes);
            inflater = new Inflater();

            buffer = new Buffer(stream, 4096);
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        internal int Read(byte[] bytes, int offset, int count)
        {
            while (true)
            {
                if (!readHeader)
                {
                    try
                    {
                        if (!ReadHeader()) return 0;
                    }
                    catch
                    {
                        return 0;
                    }
                }

                int bytesRead = ReadBody(bytes, offset, count);
                if (bytesRead > 0)
                {
                    int o = offset;
                    for (int i = 0; i < bytesRead; ++i) crc.Update(bytes[o++]);
                }
                if (inflater.IsFinished) ReadFooter();
                if (bytesRead > 0) return bytesRead;
            }
        }

        internal int ReadBody(byte[] bytes, int offset, int count)
        {
            int remainingBytes = count;
            while (true)
            {
                int bytesRead = inflater.Inflate(bytes, offset, remainingBytes);
                offset += bytesRead;
                remainingBytes -= bytesRead;

                if (remainingBytes == 0 || inflater.IsFinished) break;
                if (!inflater.input.isNeedingInput) continue;

                if (buffer.available <= 0) buffer.Fill();
                buffer.SetInflaterInput(inflater);
            }
            return count - remainingBytes;
        }

        private void ReadFooter()
        {
            byte[] footer = new byte[8];

            buffer.available += inflater.input.availableBytes;
            inflater.Reset();

            int needed = 8;
            while (needed > 0)
            {
                int count = buffer.ReadClearTextBuffer(footer, 8 - needed, needed);
                needed -= count;
            }

            readHeader = false;
        }

        private bool ReadHeader()
        {
            crc = new Crc32();

            if (buffer.available <= 0)
            {
                buffer.Fill();
                if (buffer.available <= 0) return false;
            }

            Crc32 headCRC = new Crc32();
            int magic = buffer.ReadLeByte();

            headCRC.Update(magic);
            magic = buffer.ReadLeByte();

            headCRC.Update(magic);

            int compressionType = buffer.ReadLeByte();

            headCRC.Update(compressionType);

            int flags = buffer.ReadLeByte();
            headCRC.Update(flags);

            for (int i = 0; i < 6; i++)
            {
                int readByte = buffer.ReadLeByte();
                headCRC.Update(readByte);
            }

            if ((flags & 0x4) != 0)
            {
                int len1 = buffer.ReadLeByte();
                int len2 = buffer.ReadLeByte();
                headCRC.Update(len1);
                headCRC.Update(len2);

                int extraLen = (len2 << 8) | len1;
                for (int i = 0; i < extraLen; i++)
                {
                    int readByte = buffer.ReadLeByte();
                    headCRC.Update(readByte);
                }
            }

            if ((flags & 0x8) != 0)
            {
                int readByte;
                while ((readByte = buffer.ReadLeByte()) > 0) headCRC.Update(readByte);
                headCRC.Update(readByte);
            }

            if ((flags & 0x10) != 0)
            {
                int readByte;
                while ((readByte = buffer.ReadLeByte()) > 0) headCRC.Update(readByte);
                headCRC.Update(readByte);
            }

            if ((flags & 0x2) != 0)
            {
                buffer.ReadLeByte();
                buffer.ReadLeByte();
            }

            readHeader = true;
            return true;
        }
    }

    internal class Output
    {
        private const int MASK = SIZE - 1;
        private const int SIZE = 1 << 15;

        private int end;
        private int filled;
        private byte[] window = new byte[SIZE];

        internal int available
        {
            get { return filled; }
        }

        internal int freeSpace
        {
            get { return SIZE - filled; }
        }

        internal int CopyOutput(byte[] output, int offset, int len)
        {
            int copyEnd = end;
            if (len > filled) len = filled;
            else copyEnd = (end - filled + len) & MASK;

            int copied = len;
            int tailLen = len - copyEnd;

            if (tailLen > 0)
            {
                Array.Copy(window, SIZE - tailLen, output, offset, tailLen);
                offset += tailLen;
                len = copyEnd;
            }

            Array.Copy(window, copyEnd - len, output, offset, len);
            filled -= copied;
            return copied;
        }

        internal int CopyStored(StreamManipulator input, int length)
        {
            length = Math.Min(Math.Min(length, SIZE - filled), input.availableBytes);
            int copied;

            int tailLen = SIZE - end;
            if (length > tailLen)
            {
                copied = input.CopyBytes(window, end, tailLen);
                if (copied == tailLen) copied += input.CopyBytes(window, 0, length - tailLen);
            }
            else copied = input.CopyBytes(window, end, length);

            end = (end + copied) & MASK;
            filled += copied;
            return copied;
        }

        internal void Repeat(int length, int distance)
        {
            filled += length;

            int repStart = (end - distance) & MASK;
            int border = SIZE - length;
            if (repStart <= border && end < border)
            {
                if (length <= distance)
                {
                    Array.Copy(window, repStart, window, end, length);
                    end += length;
                }
                else
                {
                    while (length-- > 0) window[end++] = window[repStart++];
                }
            }
            else SlowRepeat(repStart, length);
        }

        internal void Reset()
        {
            filled = end = 0;
        }

        private void SlowRepeat(int repStart, int length)
        {
            while (length-- > 0)
            {
                window[end++] = window[repStart++];
                end &= MASK;
                repStart &= MASK;
            }
        }

        internal void Write(int value)
        {
            filled++;
            window[end++] = (byte)value;
            end &= MASK;
        }
    }

    internal class StreamManipulator
    {
        private int bitsInBuffer;
        private uint buffer;
        private byte[] window;
        private int windowEnd;
        private int windowStart;

        internal int availableBits
        {
            get { return bitsInBuffer; }
        }

        internal int availableBytes
        {
            get { return windowEnd - windowStart + (bitsInBuffer >> 3); }
        }

        internal bool isNeedingInput
        {
            get { return windowStart == windowEnd; }
        }

        internal int CopyBytes(byte[] output, int offset, int length)
        {
            int count = 0;
            while (bitsInBuffer > 0 && length > 0)
            {
                output[offset++] = (byte)buffer;
                buffer >>= 8;
                bitsInBuffer -= 8;
                length--;
                count++;
            }

            if (length == 0) return count;

            int avail = windowEnd - windowStart;
            if (length > avail) length = avail;

            Array.Copy(window, windowStart, output, offset, length);
            windowStart += length;

            if (((windowStart - windowEnd) & 1) != 0)
            {
                buffer = (uint)(window[windowStart++] & 0xff);
                bitsInBuffer = 8;
            }
            return count + length;
        }

        internal void DropBits(int bitCount)
        {
            buffer >>= bitCount;
            bitsInBuffer -= bitCount;
        }

        internal int PeekBits(int bitCount)
        {
            if (bitsInBuffer < bitCount)
            {
                if (windowStart == windowEnd) return -1;
                buffer |= (uint)((window[windowStart++] & 0xff |
                                   (window[windowStart++] & 0xff) << 8) << bitsInBuffer);
                bitsInBuffer += 16;
            }
            return (int)(buffer & ((1 << bitCount) - 1));
        }

        internal void Reset()
        {
            buffer = 0;
            windowStart = windowEnd = bitsInBuffer = 0;
        }

        internal void SetInput(byte[] bytes, int offset, int count)
        {
            int end = offset + count;

            if ((count & 1) != 0)
            {
                buffer |= (uint)((bytes[offset++] & 0xff) << bitsInBuffer);
                bitsInBuffer += 8;
            }

            window = bytes;
            windowStart = offset;
            windowEnd = end;
        }

        internal void SkipToByteBoundary()
        {
            buffer >>= bitsInBuffer & 7;
            bitsInBuffer &= ~7;
        }
    }
}