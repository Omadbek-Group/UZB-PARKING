namespace UzbCarParking.Core
{
    /// <summary>Loyiha bo‘ylab ishlatiladigan doimiy qiymatlar.</summary>
    public static class GameConstants
    {
        public const string GameTitle = "UZB CAR PARKING";
        public const string GameSubtitle = "Avtodrom & Open World";
        public const string Version = "0.1.0-mvp";

        public const string MainMenuScene = "MainMenu";
        public const string AvtodromScene = "Avtodrom";

        public const float MsToKmh = 3.6f;

        // Avtodrom mashqlari identifikatorlari (ExamConfig.examOrder ham shularni ishlatadi)
        public const string ExerciseEstakada = "estakada";
        public const string ExerciseParallel = "parallel";
        public const string ExerciseBox = "box";
    }
}
