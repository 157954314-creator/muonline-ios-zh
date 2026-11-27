#nullable enable
using System;
using System.Collections.Generic;
using Client.Main.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Client.Main.Core.Client
{
    /// <summary>
    /// Quest state matching SourceMain CSQuest quest states.
    /// </summary>
    public enum QuestState
    {
        Undefined = 0,
        InProgress = 1,
        Complete = 2,
        Failed = 3,
    }

    /// <summary>
    /// Central quest state manager.
    /// Tracks quest progression and provides quest information.
    /// Equivalent to SourceMain CSQuest + QuestMng.
    /// </summary>
    public class QuestManager
    {
        private readonly ILogger<QuestManager> _logger;

        /// <summary>QuestIndex → current state.</summary>
        private readonly Dictionary<byte, QuestState> _questStates = new();

        /// <summary>Kill tracking: monsterType → count killed.</summary>
        private readonly Dictionary<int, int> _killCounts = new();

        /// <summary>Current active quest index being viewed.</summary>
        private byte _currentQuestIndex;

        public event Action? QuestStateChanged;

        public QuestManager(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<QuestManager>();
        }

        /// <summary>
        /// Sets the quest list from server (bitmask).
        /// Equivalent: SourceMain CSQuest::setQuestLists.
        /// </summary>
        public void SetQuestLists(byte[] list, int count)
        {
            _questStates.Clear();
            _killCounts.Clear();

            for (int i = 0; i < count; i++)
            {
                _questStates[(byte)i] = QuestState.Undefined;
            }

            _logger?.LogDebug("Quest list set: {Count} quests", count);
        }

        /// <summary>
        /// Updates a single quest state.
        /// </summary>
        public void SetQuestState(byte questIndex, QuestState state)
        {
            _questStates[questIndex] = state;
            _logger?.LogDebug("Quest {Index} state = {State}", questIndex, state);
            QuestStateChanged?.Invoke();
        }

        /// <summary>
        /// Gets current state for a quest.
        /// </summary>
        public QuestState GetQuestState(byte questIndex)
        {
            return _questStates.TryGetValue(questIndex, out var state) ? state : QuestState.Undefined;
        }

        /// <summary>
        /// Sets the currently viewed quest.
        /// </summary>
        public void SetCurrentQuest(byte index)
        {
            _currentQuestIndex = index;
        }

        public byte GetCurrentQuestIndex() => _currentQuestIndex;

        /// <summary>
        /// Updates kill count for a monster type.
        /// Equivalent: SourceMain CSQuest::SetKillMobInfo.
        /// </summary>
        public void SetKillCount(int monsterType, int count)
        {
            _killCounts[monsterType] = count;
            _logger?.LogDebug("Kill count: Monster={Type}, Count={Count}", monsterType, count);
        }

        /// <summary>
        /// Gets kill count for a monster type.
        /// </summary>
        public int GetKillCount(int monsterType)
        {
            return _killCounts.TryGetValue(monsterType, out var count) ? count : 0;
        }

        /// <summary>
        /// Gets quest NPC name by quest index.
        /// Equivalent: SourceMain CSQuest::GetNPCName.
        /// </summary>
        public string GetNpcName(byte questIndex)
        {
            return questIndex switch
            {
                // Class change quests
                0 => "圣导士塞维娜",
                1 => "警卫队长马隆",
                2 => "弟子黛彬",
                3 => "弟子黛彬",
                // Default
                _ => $"任务 NPC #{questIndex}"
            };
        }

        /// <summary>
        /// Gets quest title by index.
        /// </summary>
        public string GetQuestTitle(byte questIndex)
        {
            return questIndex switch
            {
                0 => "第一次转职任务",
                1 => "第二次转职任务",
                2 => "第三次转职任务（第一阶段）",
                3 => "第三次转职任务（第二阶段）",
                _ => $"任务 {questIndex}"
            };
        }

        /// <summary>
        /// Gets quest requirements as human-readable text.
        /// </summary>
        public string GetQuestRequirements(byte questIndex)
        {
            return questIndex switch
            {
                0 => "达到 150 级\n收集帝王之书",
                1 => "达到 220 级\n收集马隆任务物品",
                2 => "达到 380 级\n收集三件神圣遗物",
                3 => "达到 400 级\n击败暗黑精灵\n向弟子黛彬复命",
                _ => "未知条件"
            };
        }

        /// <summary>
        /// Gets quest reward text.
        /// </summary>
        public string GetQuestReward(byte questIndex)
        {
            return questIndex switch
            {
                0 => "一转奖励：解锁二阶职业与新技能",
                1 => "二转奖励：每级额外属性点与连击技能",
                2 => "三转奖励：开启大师试炼（第一阶段）",
                3 => "三转奖励：晋升大师职业（第二阶段）",
                _ => "未知奖励"
            };
        }

        /// <summary>
        /// Resets all quest state (on map change).
        /// </summary>
        public void Reset()
        {
            _questStates.Clear();
            _killCounts.Clear();
            _currentQuestIndex = 0;
        }
    }
}
