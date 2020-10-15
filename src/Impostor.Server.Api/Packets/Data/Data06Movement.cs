using Impostor.Server.Net.Messages;

namespace Impostor.Server.Net.Data
{
    public class Data06Movement
    {
        private readonly FloatRange XRange = new FloatRange(-40f, 40f);
        private readonly FloatRange YRange = new FloatRange(-40f, 40f);

        public Data06Movement(IMessageReader reader)
        {
            // https://gist.github.com/codyphobe/e454ae322ac887cd7497cfa2a0444a35#reading-the-coordinates-and-velocity
            float x = (float) (int) reader.ReadUInt16() / 65535f;
            float y = (float) (int) reader.ReadUInt16() / 65535f;

            PositionVector = new Vector2(XRange.Lerp(x), YRange.Lerp(y));

            float xV = (float) (int) reader.ReadUInt16() / 65535f;
            float yV = (float) (int) reader.ReadUInt16() / 65535f;

            VelocityVector = new Vector2(XRange.Lerp(xV), YRange.Lerp(yV));
        }

        public Vector2 PositionVector { get; }

        public Vector2 VelocityVector { get; }
    }

    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public class FloatRange
    {
        public float min;
        public float max;

        public FloatRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public float Lerp(float v)
        {
            if (v < 0f)
            {
                v = 0f;
            }
            else if (v > 1f)
            {
                v = 1f;
            }

            return this.min + (this.max - this.min) * v;
        }
    }
}