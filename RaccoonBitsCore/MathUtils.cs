namespace RaccoonBitsCore
{
    public class MathUtils
    {
        public static int Normalize(int value, int minInput, int maxInput, int minOutput, int maxOutput)
        {
            // Ensure the value is within the input range
            value = Math.Max(minInput, Math.Min(maxInput, value));

            // Perform linear normalization
            double normalizedValue = (value - minInput) / (double)(maxInput - minInput) * (maxOutput - minOutput) + minOutput;

            return (int)normalizedValue;
        }
    }
}