using Hamburgerz.Models;

namespace Hamburgerz.Services
{
    public enum MeasurementQuestionGroup
    {
        AcuteWork,
        FatigueEnergy,
        Recovery,
        ClientContact
    }

    public enum MeasurementQuestionScale
    {
        Frequency,
        Degree
    }

    public sealed class MeasurementQuestionDefinition
    {
        public string Key { get; init; } = string.Empty;

        public string PromptKey { get; init; } = string.Empty;

        public string SubcopyKey { get; init; } = string.Empty;

        public MeasurementQuestionGroup Group { get; init; }

        public MeasurementQuestionScale Scale { get; init; }

        public bool IsReverseScored { get; init; }

        public int CadenceDays { get; init; }

        public int UnreliableAfterDays { get; init; }

        public double BurnoutWeight { get; init; } = 1d;

        public double ProductivityWeight { get; init; }

    }

    public class MeasurementQuestionCatalog
    {
        public IReadOnlyList<MeasurementQuestionDefinition> Questions { get; } =
        [
            new()
            {
                Key = "tired_general",
                PromptKey = "title1descr1",
                SubcopyKey = "sub_tired_general",
                Group = MeasurementQuestionGroup.FatigueEnergy,
                Scale = MeasurementQuestionScale.Frequency,
                CadenceDays = 2,
                UnreliableAfterDays = 5,
                BurnoutWeight = 0.9,
                ProductivityWeight = 0.15
            },
            new()
            {
                Key = "physical_exhausted",
                PromptKey = "title2descr1",
                SubcopyKey = "sub_physical_exhausted",
                Group = MeasurementQuestionGroup.FatigueEnergy,
                Scale = MeasurementQuestionScale.Frequency,
                CadenceDays = 2,
                UnreliableAfterDays = 5,
                BurnoutWeight = 0.9,
                ProductivityWeight = 0.1
            },
            new()
            {
                Key = "psych_exhausted",
                PromptKey = "title3descr1",
                SubcopyKey = "sub_psych_exhausted",
                Group = MeasurementQuestionGroup.FatigueEnergy,
                Scale = MeasurementQuestionScale.Frequency,
                CadenceDays = 2,
                UnreliableAfterDays = 5,
                BurnoutWeight = 1.05,
                ProductivityWeight = 0.12
            },
            new()
            {
                Key = "cant_take_more",
                PromptKey = "q_cant_take_more",
                SubcopyKey = "sub_cant_take_more",
                Group = MeasurementQuestionGroup.AcuteWork,
                Scale = MeasurementQuestionScale.Frequency,
                CadenceDays = 1,
                UnreliableAfterDays = 3,
                BurnoutWeight = 1.35,
                ProductivityWeight = 0.18
            },
            new()
            {
                Key = "worn_out",
                PromptKey = "q_worn_out",
                SubcopyKey = "sub_worn_out",
                Group = MeasurementQuestionGroup.AcuteWork,
                Scale = MeasurementQuestionScale.Frequency,
                CadenceDays = 1,
                UnreliableAfterDays = 3,
                BurnoutWeight = 1.25,
                ProductivityWeight = 0.18
            },
            new()
            {
                Key = "weak_or_ill",
                PromptKey = "q_weak_or_ill",
                SubcopyKey = "sub_weak_or_ill",
                Group = MeasurementQuestionGroup.FatigueEnergy,
                Scale = MeasurementQuestionScale.Frequency,
                CadenceDays = 3,
                UnreliableAfterDays = 7,
                BurnoutWeight = 0.8,
                ProductivityWeight = 0.08
            },
            new()
            {
                Key = "end_day_exhausted",
                PromptKey = "title7descr1",
                SubcopyKey = "sub_end_day_exhausted",
                Group = MeasurementQuestionGroup.AcuteWork,
                Scale = MeasurementQuestionScale.Degree,
                CadenceDays = 1,
                UnreliableAfterDays = 3,
                BurnoutWeight = 1.35,
                ProductivityWeight = 0.2
            },
            new()
            {
                Key = "morning_exhausted",
                PromptKey = "title8descr1",
                SubcopyKey = "sub_morning_exhausted",
                Group = MeasurementQuestionGroup.FatigueEnergy,
                Scale = MeasurementQuestionScale.Degree,
                CadenceDays = 2,
                UnreliableAfterDays = 5,
                BurnoutWeight = 1.1,
                ProductivityWeight = 0.12
            },
            new()
            {
                Key = "every_hour_tiring",
                PromptKey = "title9descr1",
                SubcopyKey = "sub_every_hour_tiring",
                Group = MeasurementQuestionGroup.AcuteWork,
                Scale = MeasurementQuestionScale.Degree,
                CadenceDays = 2,
                UnreliableAfterDays = 4,
                BurnoutWeight = 1.15,
                ProductivityWeight = 0.22
            },
            new()
            {
                Key = "energy_for_close_people",
                PromptKey = "title10descr1",
                SubcopyKey = "sub_energy_for_close_people",
                Group = MeasurementQuestionGroup.Recovery,
                Scale = MeasurementQuestionScale.Frequency,
                IsReverseScored = true,
                CadenceDays = 3,
                UnreliableAfterDays = 7,
                BurnoutWeight = 1.05,
                ProductivityWeight = 0.1
            },
            new()
            {
                Key = "work_emotionally_exhausting",
                PromptKey = "title11descr1",
                SubcopyKey = "sub_work_emotionally_exhausting",
                Group = MeasurementQuestionGroup.AcuteWork,
                Scale = MeasurementQuestionScale.Frequency,
                CadenceDays = 1,
                UnreliableAfterDays = 3,
                BurnoutWeight = 1.35,
                ProductivityWeight = 0.18
            },
            new()
            {
                Key = "work_frustrating",
                PromptKey = "title12descr1",
                SubcopyKey = "sub_work_frustrating",
                Group = MeasurementQuestionGroup.AcuteWork,
                Scale = MeasurementQuestionScale.Frequency,
                CadenceDays = 1,
                UnreliableAfterDays = 3,
                BurnoutWeight = 1.2,
                ProductivityWeight = 0.22
            },
            new()
            {
                Key = "burned_out_by_work",
                PromptKey = "title13descr1",
                SubcopyKey = "sub_burned_out_by_work",
                Group = MeasurementQuestionGroup.AcuteWork,
                Scale = MeasurementQuestionScale.Frequency,
                CadenceDays = 1,
                UnreliableAfterDays = 3,
                BurnoutWeight = 1.45,
                ProductivityWeight = 0.18
            },
            new()
            {
                Key = "clients_hard",
                PromptKey = "title14descr1",
                SubcopyKey = "sub_clients_hard",
                Group = MeasurementQuestionGroup.ClientContact,
                Scale = MeasurementQuestionScale.Degree,
                CadenceDays = 4,
                UnreliableAfterDays = 10,
                BurnoutWeight = 0.8,
                ProductivityWeight = 0.1
            },
            new()
            {
                Key = "clients_drain_energy",
                PromptKey = "title15descr1",
                SubcopyKey = "sub_clients_drain_energy",
                Group = MeasurementQuestionGroup.ClientContact,
                Scale = MeasurementQuestionScale.Degree,
                CadenceDays = 4,
                UnreliableAfterDays = 10,
                BurnoutWeight = 0.95,
                ProductivityWeight = 0.1
            },
            new()
            {
                Key = "clients_frustrating",
                PromptKey = "title16descr1",
                SubcopyKey = "sub_clients_frustrating",
                Group = MeasurementQuestionGroup.ClientContact,
                Scale = MeasurementQuestionScale.Degree,
                CadenceDays = 4,
                UnreliableAfterDays = 10,
                BurnoutWeight = 0.9,
                ProductivityWeight = 0.12
            },
            new()
            {
                Key = "clients_give_more",
                PromptKey = "title17descr1",
                SubcopyKey = "sub_clients_give_more",
                Group = MeasurementQuestionGroup.ClientContact,
                Scale = MeasurementQuestionScale.Degree,
                CadenceDays = 4,
                UnreliableAfterDays = 10,
                BurnoutWeight = 0.85,
                ProductivityWeight = 0.08
            },
            new()
            {
                Key = "clients_tired",
                PromptKey = "title18descr1",
                SubcopyKey = "sub_clients_tired",
                Group = MeasurementQuestionGroup.ClientContact,
                Scale = MeasurementQuestionScale.Frequency,
                CadenceDays = 4,
                UnreliableAfterDays = 10,
                BurnoutWeight = 0.9,
                ProductivityWeight = 0.1
            },
            new()
            {
                Key = "clients_continue",
                PromptKey = "title19descr1",
                SubcopyKey = "sub_clients_continue",
                Group = MeasurementQuestionGroup.ClientContact,
                Scale = MeasurementQuestionScale.Frequency,
                CadenceDays = 5,
                UnreliableAfterDays = 12,
                BurnoutWeight = 0.85,
                ProductivityWeight = 0.08
            }
        ];

        public IReadOnlyList<MeasurementQuestionDefinition> GetApplicableQuestions(User user)
        {
            return Questions.ToList();
        }

        public MeasurementQuestionDefinition? Find(string key) =>
            Questions.FirstOrDefault(question => question.Key == key);

        public List<MeasurementQuestionOptionViewModel> BuildOptions(MeasurementQuestionDefinition question)
        {
            string[] labels = question.Scale == MeasurementQuestionScale.Degree
                ? ["time5", "time4", "time3", "time2", "time1"]
                : question.IsReverseScored
                    ? ["mood1", "mood2", "mood3", "mood4", "mood5"]
                    : ["mood5", "mood4", "mood3", "mood2", "mood1"];

            return labels
                .Select((labelKey, score) => new MeasurementQuestionOptionViewModel
                {
                    Score = score,
                    LabelKey = labelKey
                })
                .ToList();
        }

        public MeasurementQuestionViewModel ToViewModel(
            MeasurementQuestionDefinition question,
            int? selectedScore,
            DateTime today,
            MeasurementAnswer? latestAnswer)
        {
            return new MeasurementQuestionViewModel
            {
                Key = question.Key,
                PromptKey = question.PromptKey,
                SubcopyKey = question.SubcopyKey,
                GroupKey = GetGroupKey(question.Group),
                FreshnessKey = GetFreshnessKey(question, today, latestAnswer),
                SelectedScore = selectedScore,
                Options = BuildOptions(question)
            };
        }

        public static string GetGroupKey(MeasurementQuestionGroup group) =>
            group switch
            {
                MeasurementQuestionGroup.AcuteWork => "carouselAcuteWork",
                MeasurementQuestionGroup.FatigueEnergy => "carouselFatigue",
                MeasurementQuestionGroup.Recovery => "carouselRecovery",
                MeasurementQuestionGroup.ClientContact => "carouselClient",
                _ => "carouselAcuteWork"
            };

        private static string GetFreshnessKey(
            MeasurementQuestionDefinition question,
            DateTime today,
            MeasurementAnswer? latestAnswer)
        {
            if (latestAnswer == null)
            {
                return "freshnessNew";
            }

            var ageDays = Math.Max(0, (today.Date - latestAnswer.AnsweredAt.Date).Days);
            if (ageDays >= question.UnreliableAfterDays)
            {
                return "freshnessUnreliable";
            }

            if (ageDays >= question.CadenceDays)
            {
                return "freshnessDue";
            }

            return "freshnessFresh";
        }

    }
}
