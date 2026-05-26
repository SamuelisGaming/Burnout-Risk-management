using Hamburgerz.Models;

namespace Hamburgerz.Services
{
    public sealed class MeasurementQuestionSelection
    {
        public IReadOnlyList<MeasurementQuestionDefinition> Questions { get; init; } = [];

        public bool IsFirstQuestionnaire { get; init; }
    }

    public sealed class MeasurementScoreResult
    {
        public int BurnoutScore { get; init; }

        public int ProductivityScore { get; init; }

        public float Coverage { get; init; }
    }

    public class MeasurementScoringService
    {
        private const int DefaultRotatingQuestionCount = 4;
        private const int MaxDynamicRotatingQuestionCount = 7;

        private readonly MeasurementQuestionCatalog _catalog;

        public MeasurementScoringService(MeasurementQuestionCatalog catalog)
        {
            _catalog = catalog;
        }

        public MeasurementQuestionSelection SelectQuestions(
            User user,
            IReadOnlyDictionary<string, MeasurementAnswer> latestAnswers,
            DateTime today,
            IReadOnlyCollection<string>? exactQuestionKeys = null)
        {
            var applicable = _catalog.GetApplicableQuestions(user);

            if (exactQuestionKeys != null && exactQuestionKeys.Count > 0)
            {
                var exact = exactQuestionKeys
                    .Select(key => applicable.FirstOrDefault(question => question.Key == key))
                    .Where(question => question != null)
                    .Cast<MeasurementQuestionDefinition>()
                    .DistinctBy(question => question.Key)
                    .ToList();

                return new MeasurementQuestionSelection
                {
                    Questions = exact,
                    IsFirstQuestionnaire = latestAnswers.Count == 0
                };
            }

            var answeredCount = applicable.Count(question => latestAnswers.ContainsKey(question.Key));
            if (answeredCount == 0)
            {
                return new MeasurementQuestionSelection
                {
                    Questions = applicable,
                    IsFirstQuestionnaire = true
                };
            }

            var missing = applicable
                .Where(question => !latestAnswers.ContainsKey(question.Key))
                .OrderByDescending(question => question.BurnoutWeight)
                .ThenBy(question => StableQuestionOrder(user.Id, today, question.Key))
                .Take(MaxDynamicRotatingQuestionCount)
                .ToList();

            if (missing.Count > 0)
            {
                return new MeasurementQuestionSelection
                {
                    Questions = missing,
                    IsFirstQuestionnaire = false
                };
            }

            var candidates = applicable
                .Select(question => new QuestionCandidate(
                    question,
                    GetAnswerAgeDays(latestAnswers[question.Key], today),
                    StableQuestionOrder(user.Id, today, question.Key)))
                .ToList();

            var unreliableCount = candidates.Count(candidate => candidate.IsUnreliable);
            var targetCount = Math.Clamp(
                DefaultRotatingQuestionCount + Math.Min(3, unreliableCount),
                DefaultRotatingQuestionCount,
                MaxDynamicRotatingQuestionCount);

            var selected = new List<MeasurementQuestionDefinition>();

            AddFromGroup(selected, candidates, MeasurementQuestionGroup.AcuteWork, 2, targetCount);
            AddFromGroup(selected, candidates, MeasurementQuestionGroup.FatigueEnergy, 1, targetCount);
            AddFromGroup(selected, candidates, MeasurementQuestionGroup.Recovery, 1, targetCount);

            if (targetCount > DefaultRotatingQuestionCount)
            {
                AddFromGroup(selected, candidates, MeasurementQuestionGroup.ClientContact, 1, targetCount);
            }

            foreach (var candidate in candidates
                .Where(candidate => selected.All(question => question.Key != candidate.Question.Key))
                .OrderByDescending(candidate => candidate.IsUnreliable)
                .ThenByDescending(candidate => candidate.IsDue)
                .ThenByDescending(candidate => candidate.StalenessRatio)
                .ThenByDescending(candidate => candidate.Question.BurnoutWeight)
                .ThenBy(candidate => candidate.StableOrder))
            {
                if (selected.Count >= targetCount)
                {
                    break;
                }

                selected.Add(candidate.Question);
            }

            return new MeasurementQuestionSelection
            {
                Questions = selected,
                IsFirstQuestionnaire = false
            };
        }

        public MeasurementScoreResult CalculateScore(
            User user,
            MeasurementEntryViewModel model,
            IReadOnlyDictionary<string, int> submittedScores,
            IReadOnlyDictionary<string, MeasurementAnswer> latestAnswers,
            DateTime today)
        {
            var dailyRisk = WeightedAverage(
                (SleepRisk(model.SleepHours), 1.15),
                (WorkHoursRisk(model.WorkHours), 1.0),
                (StressRisk(model.StressLevel), 1.25),
                (FocusRisk(model.FocusScore), 1.0),
                (DisconnectRisk(model.DisconnectScore), 0.8),
                (MoodRisk(model.MoodScore), 0.85),
                (ExerciseRisk(model.ExerciseHours), 0.55),
                (ScreenRisk(model.ScreenTime), 0.55),
                (MeetingsRisk(model.MeetingsPerDay), 0.45));

            var applicableQuestions = _catalog.GetApplicableQuestions(user);
            var questionRisk = CalculateQuestionRisk(applicableQuestions, submittedScores, latestAnswers, today, out var coverage);

            var burnoutScore = ClampToPercent((dailyRisk * 0.42) + (questionRisk * 0.58));

            var dailyProductivityRisk = WeightedAverage(
                (FocusRisk(model.FocusScore), 1.35),
                (StressRisk(model.StressLevel), 0.95),
                (MeetingsRisk(model.MeetingsPerDay), 0.75),
                (ScreenRisk(model.ScreenTime), 0.65),
                (WorkHoursRisk(model.WorkHours), 0.55),
                (SleepRisk(model.SleepHours), 0.45));

            var questionProductivityRisk = CalculateQuestionProductivityRisk(applicableQuestions, submittedScores, latestAnswers);
            var productivityRisk = (dailyProductivityRisk * 0.72) + (questionProductivityRisk * 0.28);
            var productivityScore = ClampToPercent(100 - productivityRisk);

            return new MeasurementScoreResult
            {
                BurnoutScore = burnoutScore,
                ProductivityScore = productivityScore,
                Coverage = coverage
            };
        }

        private static void AddFromGroup(
            ICollection<MeasurementQuestionDefinition> selected,
            IEnumerable<QuestionCandidate> candidates,
            MeasurementQuestionGroup group,
            int count,
            int targetCount)
        {
            foreach (var candidate in candidates
                .Where(candidate => candidate.Question.Group == group)
                .Where(candidate => selected.All(question => question.Key != candidate.Question.Key))
                .OrderByDescending(candidate => candidate.IsUnreliable)
                .ThenByDescending(candidate => candidate.IsDue)
                .ThenByDescending(candidate => candidate.StalenessRatio)
                .ThenByDescending(candidate => candidate.Question.BurnoutWeight)
                .ThenBy(candidate => candidate.StableOrder)
                .Take(count))
            {
                if (selected.Count >= targetCount)
                {
                    break;
                }

                selected.Add(candidate.Question);
            }
        }

        private static double CalculateQuestionRisk(
            IReadOnlyList<MeasurementQuestionDefinition> questions,
            IReadOnlyDictionary<string, int> submittedScores,
            IReadOnlyDictionary<string, MeasurementAnswer> latestAnswers,
            DateTime today,
            out float coverage)
        {
            var weightedRisk = 0d;
            var totalWeight = 0d;
            var weightedConfidence = 0d;

            foreach (var question in questions)
            {
                var hasSubmitted = submittedScores.TryGetValue(question.Key, out var submittedScore);
                var hasPrevious = latestAnswers.TryGetValue(question.Key, out var previousAnswer);
                var score = hasSubmitted
                    ? submittedScore
                    : hasPrevious
                        ? previousAnswer!.Score
                        : 2;

                var confidence = hasSubmitted
                    ? 1d
                    : hasPrevious
                        ? CalculateConfidence(previousAnswer!, question, today)
                        : 0d;

                var weight = question.BurnoutWeight;
                weightedRisk += (score / 4d * 100d) * weight;
                totalWeight += weight;
                weightedConfidence += confidence * weight;
            }

            coverage = totalWeight <= 0 ? 0f : (float)Math.Round(weightedConfidence / totalWeight, 2);
            return totalWeight <= 0 ? 50d : weightedRisk / totalWeight;
        }

        private static double CalculateQuestionProductivityRisk(
            IReadOnlyList<MeasurementQuestionDefinition> questions,
            IReadOnlyDictionary<string, int> submittedScores,
            IReadOnlyDictionary<string, MeasurementAnswer> latestAnswers)
        {
            var weightedRisk = 0d;
            var totalWeight = 0d;

            foreach (var question in questions.Where(question => question.ProductivityWeight > 0))
            {
                var score = submittedScores.TryGetValue(question.Key, out var submittedScore)
                    ? submittedScore
                    : latestAnswers.TryGetValue(question.Key, out var previousAnswer)
                        ? previousAnswer.Score
                        : 2;

                weightedRisk += (score / 4d * 100d) * question.ProductivityWeight;
                totalWeight += question.ProductivityWeight;
            }

            return totalWeight <= 0 ? 50d : weightedRisk / totalWeight;
        }

        private static double CalculateConfidence(
            MeasurementAnswer answer,
            MeasurementQuestionDefinition question,
            DateTime today)
        {
            var ageDays = GetAnswerAgeDays(answer, today);
            if (ageDays <= question.CadenceDays)
            {
                return 1d;
            }

            if (ageDays >= question.UnreliableAfterDays)
            {
                return 0.35d;
            }

            var staleRange = Math.Max(1, question.UnreliableAfterDays - question.CadenceDays);
            var staleProgress = (ageDays - question.CadenceDays) / (double)staleRange;
            return 1d - (staleProgress * 0.65d);
        }

        private static int GetAnswerAgeDays(MeasurementAnswer answer, DateTime today) =>
            Math.Max(0, (today.Date - answer.AnsweredAt.Date).Days);

        private static int StableQuestionOrder(int userId, DateTime today, string questionKey) =>
            HashCode.Combine(userId, today.DayOfYear, questionKey);

        private static int ClampToPercent(double value) =>
            (int)Math.Round(Math.Clamp(value, 0d, 100d));

        private static double WeightedAverage(params (double Value, double Weight)[] items)
        {
            var totalWeight = items.Sum(item => item.Weight);
            if (totalWeight <= 0)
            {
                return 0d;
            }

            return items.Sum(item => item.Value * item.Weight) / totalWeight;
        }

        private static double SleepRisk(float? value)
        {
            if (!value.HasValue) return 50d;
            if (value.Value < 5f) return 100d;
            if (value.Value < 6f) return 78d;
            if (value.Value < 7f) return 45d;
            if (value.Value <= 9f) return 12d;
            if (value.Value <= 10f) return 28d;
            return 45d;
        }

        private static double WorkHoursRisk(float? value)
        {
            if (!value.HasValue) return 50d;
            if (value.Value <= 4f) return 30d;
            if (value.Value <= 8f) return 18d;
            if (value.Value <= 9f) return 42d;
            if (value.Value <= 10f) return 62d;
            if (value.Value <= 12f) return 84d;
            return 100d;
        }

        private static double ExerciseRisk(float? value)
        {
            if (!value.HasValue) return 45d;
            if (value.Value <= 0f) return 65d;
            if (value.Value < 0.5f) return 42d;
            if (value.Value <= 2f) return 15d;
            return 22d;
        }

        private static double ScreenRisk(float? value)
        {
            if (!value.HasValue) return 45d;
            if (value.Value <= 5f) return 18d;
            if (value.Value <= 7f) return 32d;
            if (value.Value <= 9f) return 52d;
            if (value.Value <= 11f) return 76d;
            return 92d;
        }

        private static double MeetingsRisk(int? value)
        {
            if (!value.HasValue) return 35d;
            if (value.Value <= 1) return 15d;
            if (value.Value <= 3) return 32d;
            if (value.Value <= 5) return 55d;
            if (value.Value <= 7) return 75d;
            return 92d;
        }

        private static double StressRisk(string? value)
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized.Contains("auk") || normalized.Contains("high")) return 92d;
            if (normalized.Contains("vid") || normalized.Contains("med")) return 55d;
            if (normalized.Contains("\u017eem") || normalized.Contains("zem") || normalized.Contains("low")) return 15d;
            return 50d;
        }

        private static double FocusRisk(int? value)
        {
            if (!value.HasValue) return 50d;
            return value.Value switch
            {
                >= 3 => 8d,
                2 => 45d,
                _ => 88d
            };
        }

        private static double DisconnectRisk(int? value)
        {
            if (!value.HasValue) return 50d;
            return value.Value switch
            {
                >= 3 => 10d,
                2 => 45d,
                _ => 85d
            };
        }

        private static double MoodRisk(int? value)
        {
            if (!value.HasValue) return 50d;
            return value.Value switch
            {
                >= 4 => 8d,
                3 => 32d,
                2 => 58d,
                _ => 88d
            };
        }

        private sealed record QuestionCandidate(
            MeasurementQuestionDefinition Question,
            int AgeDays,
            int StableOrder)
        {
            public bool IsDue => AgeDays >= Question.CadenceDays;

            public bool IsUnreliable => AgeDays >= Question.UnreliableAfterDays;

            public double StalenessRatio => AgeDays / (double)Math.Max(1, Question.CadenceDays);
        }
    }
}
