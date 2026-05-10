using System;
using Kaligo.Database;

namespace Kaligo.Services {
    public interface IQuestService {
        QuestState GetState(string questId);
        void       SetState(string questId, QuestState state);
        event Action<string, QuestState> OnQuestStateChanged;
    }
}
