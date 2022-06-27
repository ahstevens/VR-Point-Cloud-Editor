/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Text;

public class OnlineMapsPBFReader
{
    public int tag;
    public ulong value;
    public WireTypes wireType;

    private byte[] buffer;
    private ulong length;
    private ulong position;

    public OnlineMapsPBFReader(byte[] buffer)
    {
        this.buffer = buffer;
        length = (ulong)this.buffer.Length;
        wireType = WireTypes.UNDEFINED;
    }

    public double GetDouble()
    {
        byte[] buf = new byte[8];
        Array.Copy(buffer, (int)position, buf, 0, 8);
        position += 8;
        double dblVal = BitConverter.ToDouble(buf, 0);
        return dblVal;
    }

    public float GetFloat()
    {
        byte[] buf = new byte[4];
        Array.Copy(buffer, (int)position, buf, 0, 4);
        position += 4;
        float snglVal = BitConverter.ToSingle(buf, 0);
        return snglVal;
    }

    public List<uint> GetPackedUnit32()
    {
        List<uint> values = new List<uint>(200);
        ulong sizeInByte = (ulong)Varint();
        ulong end = position + sizeInByte;
        while (position < end)
        {
            ulong val = (ulong)Varint();
            values.Add((uint)val);
        }
        return values;
    }

    public string GetString(ulong length)
    {
        byte[] buf = new byte[length];
        Array.Copy(buffer, (int)position, buf, 0, (int)length);
        position += length;
        return Encoding.UTF8.GetString(buf, 0, buf.Length);
    }

    public bool NextByte()
    {
        if (position >= length) return false;

        value = (ulong)Varint();
        tag = (int)value >> 3;
        wireType = (WireTypes)(value & 0x07);
        return true;
    }

    public void Skip()
    {
        switch (wireType)
        {
            case WireTypes.VARINT:
                Varint();
                break;
            case WireTypes.BYTES:
                ulong skip = (ulong)Varint();
                position += skip;
                break;
            case WireTypes.FIXED32:
                position += 4;
                break;
            case WireTypes.FIXED64:
                position += 8;
                break;
        }
    }

    public long Varint()
    {
        int shift = 0;
        long result = 0;
        while (shift < 64)
        {
            byte b = buffer[position];
            result |= (long)(b & 0x7F) << shift;
            position++;
            if ((b & 0x80) == 0) return result;
            shift += 7;
        }
        throw new ArgumentException("Invalid varint");

    }

    public byte[] View()
    {
        ulong skip = (ulong)Varint();

        byte[] buf = new byte[skip];
        Array.Copy(buffer, (int)position, buf, 0, (int)skip);
        position += skip;

        return buf;
    }

    public enum WireTypes
    {
        VARINT = 0,
        FIXED64 = 1,
        BYTES = 2,
        FIXED32 = 5,
        UNDEFINED = 99
    }
}