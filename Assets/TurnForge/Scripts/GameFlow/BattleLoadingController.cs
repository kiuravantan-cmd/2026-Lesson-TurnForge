using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TF.Battle.Preparation;

namespace TF.GameFlow
{
    public sealed class BattleLoadingController : IDisposable
    {
        /// <summary>
        /// ゲーム全体の状態遷移
        /// </summary>
        private readonly GameFlowController _gameFlow;

        /// <summary>
        /// アセット読み込みなどの具体的な準備処理
        /// </summary>
        private readonly IBattlePreparation _preparation;
        
        /// <summary>
        /// 実行中の準備を中断するための通知元
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// 準備処理を実行しているか
        /// </summary>
        private bool _isPreparing;
        
        /// <summary>
        /// 準備済みのリソースを保持しているか
        /// </summary>
        private bool _isPrepared;
        
        /// <summary>
        /// この管理処理が破棄されたか
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// 状態遷移と具体的な準備処理を受け取る
        /// </summary>
        public BattleLoadingController(GameFlowController gameFlow, IBattlePreparation preparation)
        {
            _gameFlow = gameFlow;
            _preparation = preparation;
        }

        public async UniTask<bool> TryPrepareAsync()
        {
            if (_isDisposed || _isPreparing || _isPrepared || _gameFlow == null || _preparation == null)
            {
                return false;
            }

            _isPreparing = true;
            _cts = new CancellationTokenSource();

            if (!_gameFlow.TryBeginBattleLoading())
            {
                EndPreparation();
                return false;
            }
            
            // 準備処理へ渡す中断通知
            CancellationToken cancellationToken = _cts.Token;
            
            // 通常の失敗とキャンセルがfalseで受け取る
            bool succeeded = await _preparation.PrepareAsync(cancellationToken);
            
            // キャンセル後に成功が返っても、戦闘開始には使用しない
            bool canContinue = succeeded &&
                               !cancellationToken.IsCancellationRequested &&
                               !_isDisposed &&
                               _gameFlow.CurrentState == GameState.BattleLoading;

            if (!canContinue)
            {
                _preparation.Release();
                EndPreparation();

                if (!_isDisposed)
                {
                    _gameFlow.TryCancelBattleLoading();
                }
                
                return false;
            }

            _isPrepared = true;
            EndPreparation();
            return true;
        }

        /// <summary>
        /// ModelやPresenterの接続完了後、戦闘画面へ進む
        /// </summary>
        public bool TryEnterBattle()
        {
            if (_isDisposed || _isPreparing || !_isPrepared)
            {
                return false;
            }
            
            return _gameFlow.TryCompleteBattleLoading();
        }

        /// <summary>
        /// 戦闘準備を中断
        /// </summary>
        public void CancelPreparation()
        {
            if (_isDisposed || _gameFlow == null || _gameFlow.CurrentState != GameState.BattleLoading)
            {
                return;
            }

            if (_isPreparing)
            {
                _cts?.Cancel();
                return;
            }

            // 準備完了後、戦闘に移る前の中断
            ReleasePreparatedResources();
            _gameFlow.TryCancelBattleLoading();
        }

        /// <summary>
        /// 利用側を終了させた後、戦闘用リソースを解放
        /// </summary>
        public bool ReleasePreparatedResources()
        {
            if (_isPreparing)
            {
                return false;
            }

            if (_isPrepared)
            {
                _preparation.Release();
                _isPrepared = false;
            }
            
            return true;
        }

        /// <summary>
        /// 準備の実行状態と中断通知元を終了させる
        /// </summary>
        private void EndPreparation()
        {
            _cts?.Dispose();
            _cts = null;
            _isPreparing = false;
        }

        /// <summary>
        /// 新しい操作を禁止し、準備の中断またはリソース解放を行う
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            
            _isDisposed = true;

            if (_isPreparing)
            {
                _cts?.Cancel();
                return;
            }

            ReleasePreparatedResources();
        }
    }
}