namespace UzbCarParking.Core
{
    public enum AvtodromMode
    {
        FullExam = 0,
        SingleExercise = 1
    }

    /// <summary>
    /// Sahnalar orasida tanlangan rejimni uzatadi (Bosh menyu → Avtodrom).
    /// </summary>
    public static class SessionContext
    {
        public static AvtodromMode Mode { get; private set; } = AvtodromMode.FullExam;
        public static string ExerciseId { get; private set; } = string.Empty;

        public static void SelectFullExam()
        {
            Mode = AvtodromMode.FullExam;
            ExerciseId = string.Empty;
        }

        public static void SelectExercise(string exerciseId)
        {
            if (string.IsNullOrEmpty(exerciseId))
            {
                SelectFullExam();
                return;
            }

            Mode = AvtodromMode.SingleExercise;
            ExerciseId = exerciseId;
        }
    }
}
