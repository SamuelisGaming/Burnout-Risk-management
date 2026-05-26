using System.ComponentModel.DataAnnotations;

namespace Hamburgerz.Models
{
    public class MeasurementEntryViewModel
    {
        public DateTime? BirthDate { get; set; }


        public string Gender { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string JobRole { get; set; } = string.Empty;

        public int? ExperienceYears { get; set; }

        public string CompanySize { get; set; } = string.Empty;

        public string WorkEnvironment { get; set; } = string.Empty;

        [Required(ErrorMessage = "Įrašykite darbo valandas")]
        public float? WorkHours { get; set; }

        [Required(ErrorMessage = "Įrašykite susirinkimų skaičių")]
        public int? MeetingsPerDay { get; set; }

        //[Required(ErrorMessage = "Įrašykite interneto greitį")]
        public int? InternetSpeed { get; set; }

        [Required(ErrorMessage = "Įrašykite miego valandas")]
        public float? SleepHours { get; set; }

        [Required(ErrorMessage = "Įrašykite sporto valandas")]
        public float? ExerciseHours { get; set; }

        [Required(ErrorMessage = "Įrašykite ekrano valandas")]
        public float? ScreenTime { get; set; }

        [Required(ErrorMessage = "Pažymėkite streso lygį")]
        public string StressLevel { get; set; } = string.Empty;

        public List<int> Q { get; set; } = new();

        public List<MeasurementQuestionViewModel> Questions { get; set; } = new();

        public List<string> QuestionKeys { get; set; } = new();

        public Dictionary<string, int?> QuestionScores { get; set; } = new();

        public bool IsFirstQuestionnaire { get; set; }

        public int DailyQuestionCount { get; set; }

        public int RotatingQuestionCount => Questions.Count;

        public int TotalVisibleQuestionCount => DailyQuestionCount + RotatingQuestionCount;

        public string QuestionOrderSeed { get; set; } = string.Empty;


        public int? MoodScore { get; set; }
        public int? DisconnectScore { get; set; }
        public int? FocusScore { get; set; }

        public string UserType { get; set; } = "user";

        public int MeasurementCount { get; set; }

        public int? MeasurementLimit { get; set; }

        public bool CanCreateMeasurement { get; set; } = true;

        public int? ExistingMeasurementId { get; set; }

        public bool IsEditingTodayMeasurement { get; set; }

        public DateTime? ExistingMeasurementTimeStamp { get; set; }

        public int RemainingMeasurements =>
            MeasurementLimit.HasValue
                ? Math.Max(0, MeasurementLimit.Value - MeasurementCount)
                : int.MaxValue;

        public bool HasAnyProfileData =>
            BirthDate.HasValue
            || !string.IsNullOrWhiteSpace(Gender)
            || !string.IsNullOrWhiteSpace(Country)
            || !string.IsNullOrWhiteSpace(JobRole)
            || ExperienceYears.HasValue
            || !string.IsNullOrWhiteSpace(CompanySize)
            || !string.IsNullOrWhiteSpace(WorkEnvironment);

    }

    public class MeasurementQuestionViewModel
    {
        public string Key { get; set; } = string.Empty;

        public string PromptKey { get; set; } = string.Empty;

        public string SubcopyKey { get; set; } = string.Empty;

        public string GroupKey { get; set; } = string.Empty;

        public string FreshnessKey { get; set; } = string.Empty;

        public int? SelectedScore { get; set; }

        public List<MeasurementQuestionOptionViewModel> Options { get; set; } = new();
    }

    public class MeasurementQuestionOptionViewModel
    {
        public int Score { get; set; }

        public string LabelKey { get; set; } = string.Empty;
    }
}
