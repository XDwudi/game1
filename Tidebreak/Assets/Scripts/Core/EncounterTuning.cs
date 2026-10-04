using UnityEngine;

namespace Tidebreak
{
    // Mechanic rewards and recoverable mistakes, rather than eleven identical
    // shield/HP gates. Base HP stays in the species catalog for this revision.
    public static class EncounterTuning
    {
        static readonly float[] ExposureSeconds = { 10, 8, 12, 9, 8, 10, 7, 12, 9, 10, 12 };
        static readonly float[] ExposureBonus = { 1.35f, 1.35f, 1.15f, 1.4f, 1.5f, 1.3f, 1.5f, 1.25f, 1.4f, 1.3f, 1.45f };
        static readonly float[] MistakeBase = { 15, 6, 14, 10, 10, 14, 16, 18, 12, 18, 22 };
        static readonly float[] MistakePerPhase = { 3, 1, 2, 2, 2, 2, 2, 1, 2, 2, 2 };
        static readonly float[] PressureSeconds = { 7.5f, 8, 8, 8, 8, 7.5f, 8.5f, 8.5f, 8, 7.5f, 8 };

        static int Index(int bossIndex) { return Mathf.Clamp(bossIndex, 0, 10); }
        static int Phase(int phase) { return Mathf.Clamp(phase, 1, 3); }

        public static float ExposureDuration(int bossIndex, int phase)
        {
            // Later phases have longer mechanics, so their successful reward
            // grows slightly. It is not a compulsory wait or a clear-time floor.
            return ExposureSeconds[Index(bossIndex)] + (Phase(phase) - 1) * .5f;
        }
        public static float ExposureMultiplier(int bossIndex)
        {
            return ExposureBonus[Index(bossIndex)];
        }
        public static float FailureDamage(int bossIndex, int phase)
        {
            int index = Index(bossIndex);
            return MistakeBase[index] + MistakePerPhase[index] * Phase(phase);
        }
        public static float PressureInterval(int bossIndex, int phase)
        {
            return PressureSeconds[Index(bossIndex)] - (Phase(phase) - 1) * .6f;
        }
    }
}
