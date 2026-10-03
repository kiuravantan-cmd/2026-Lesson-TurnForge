using System;

namespace TF.GameFlow
{
    /// <summary>
    /// ゲーム全体の状態と、許可された画面遷移を管理
    /// </summary>
    public sealed class GameFlowController
    {
        /// <summary>
        /// 状態変更を通知しているか
        /// </summary>
        private bool _isNotifying = false;

        /// <summary>
        /// 現在の進行段階
        /// </summary>
        public GameState CurrentState { get; private set; } = GameState.Inactive;
        
        /// <summary>
        /// 状態が変更された後に通知するイベント
        /// </summary>
        public event Action<GameState> StateChanged;

        /// <summary>
        /// 起動時に読み込みを開始
        /// </summary>
        public bool TryBeginStartup()
        {
            return TryTransition(GameState.Inactive, GameState.Loading);
        }
        
        /// <summary>
        /// 起動準備を完了し、タイトルへ進む
        /// </summary>
        public bool TryCompleteStartup()
        {
            return TryTransition(GameState.Loading, GameState.Title);
        }

        /// <summary>
        /// 起動準備の失敗を確定する
        /// </summary>
        public bool TryFailStartup()
        {
            return TryTransition(GameState.Loading, GameState.Error);
        }

        /// <summary>
        /// タイトルまたは結果画面から、戦闘準備を開始
        /// </summary>
        public bool TryBeginBattleLoading()
        {
            return CurrentState switch
            {
                GameState.Title or GameState.Result => TryTransition(CurrentState, GameState.BattleLoading),
                _ => false
            };
        }

        /// <summary>
        /// 必要な準備が全て完了した後、戦闘へ進む
        /// </summary>
        public bool TryCompleteBattleLoading()
        {
            return TryTransition(GameState.BattleLoading, GameState.Battle);
        }

        /// <summary>
        /// 準備の中断または失敗後、タイトルへ戻る
        /// </summary>
        public bool TryCancelBattleLoading()
        {
            return TryTransition(GameState.BattleLoading, GameState.Title);
        }

        /// <summary>
        /// 戦闘と最後の演出が完了した後、結果画面へ進む
        /// </summary>
        public bool TryShowResult()
        {
            return TryTransition(GameState.Battle, GameState.Result);
        }

        /// <summary>
        /// 結果画面からタイトルへ戻る
        /// </summary>
        public bool TryReturnToTitle()
        {
            return TryTransition(GameState.Result, GameState.Title);
        }

        /// <summary>
        /// 現在の状態を確認して遷移し、変更を通知
        /// </summary>
        private bool TryTransition(GameState expectedState, GameState nextState)
        {
            if (_isNotifying || CurrentState != expectedState)
            {
                return false;
            }
            
            CurrentState = nextState;
            
            // 通知中に別の遷移が入り、通知順が崩れることを防ぐ
            _isNotifying = true;
            StateChanged?.Invoke(CurrentState);
            _isNotifying = false;

            return true;
        }
    }
}