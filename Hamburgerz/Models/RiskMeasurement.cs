using System;

namespace Hamburgerz.Models
{
    public class RiskMeasurement
    {
        public int ID { get; set; }
        public DateTime TimeStamp { get; set; }

        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? JobRole { get; set; }
        public int? ExperienceYears { get; set; }
        public string? CompanySize { get; set; }
        public float WorkHours { get; set; }
        public int MeetingsPerDay { get; set; }
        public int InternetSpeed { get; set; }
        public string? WorkEnvironment { get; set; }
        public float SleepHours { get; set; }
        public float ExerciseHours { get; set; }
        public float ScreenTime { get; set; }
        public string StressLevel { get; set; } = string.Empty;
        public int ProductivityScore { get; set; }
        public float BurnoutRisk { get; set; }

        public int BurnoutPercent
        {
            get
            {
                if (BurnoutRisk <= 1f)
                    return (int)Math.Round(BurnoutRisk * 100);

                return (int)Math.Round(BurnoutRisk);
            }
        }

        public string RiskText
        {
            get
            {
                if (BurnoutPercent >= 70) return "High";
                if (BurnoutPercent >= 40) return "Medium";
                return "Low";
            }
        }

        public string RiskClass
        {
            get
            {
                if (BurnoutPercent >= 70) return "risk-high";
                if (BurnoutPercent >= 40) return "risk-medium";
                return "risk-low";
            }
        }

        public string SleepStatus
        {
            get
            {
                if (SleepHours < 6) return "Too little";
                if (SleepHours <= 8) return "Good";
                return "Above average";
            }
        }

        public string WorkloadStatus
        {
            get
            {
                if (WorkHours >= 10) return "Heavy";
                if (WorkHours >= 8) return "Normal";
                return "Light";
            }
        }

        public string ProductivityStatus
        {
            get
            {
                if (ProductivityScore >= 80) return "Very good";
                if (ProductivityScore >= 60) return "Good";
                if (ProductivityScore >= 40) return "Average";
                return "Low";
            }
        }

        public string ExerciseStatus
        {
            get
            {
                if (ExerciseHours <= 0) return "None";
                if (ExerciseHours < 1) return "Low";
                if (ExerciseHours <= 2) return "Good";
                return "High";
            }
        }

        public string QuickSummary
        {
            get
            {
                return $"{RiskText} risk, {StressLevel} stress, {SleepHours:0.#}h sleep, {WorkHours:0.#}h work, {ProductivityScore} productivity";
            }
        }

        public string? AISummary { get; set; }
    }
}
