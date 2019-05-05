using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DispatcherHelper
{
    /// <summary>
    /// イベントループクラス。
    ///
    /// ワーカースレッドで、順番に処理を行う。
    /// </summary>
    public sealed class EventLoop : IDisposable
    {
        #region 定数

        /// <summary>
        /// オブジェクトの状態。
        /// </summary>
        private enum State
        {
            /// <summary>
            /// <see cref="Run"/>実行前。
            /// </summary>
            NotRunning,

            /// <summary>
            /// <see cref="Run"/>実行後。
            /// </summary>
            Running,

            /// <summary>
            /// <see cref="Dispose"/>実行中。
            /// </summary>
            Disposing,

            /// <summary>
            /// <see cref="Dispose"/>実行後。
            /// </summary>
            Disposed,
        }

        #endregion  // 定数


        #region フィールド

        /// <summary>
        /// 処理を行うワーカースレッド。
        /// </summary>
        private Thread loopThread_;

        #endregion  // フィールド


        #region プロパティ

        /// <summary>
        /// オブジェクトの状態。
        /// </summary>
        private int status_;
        /// <summary>
        /// オブジェクトの状態を取得する。
        /// </summary>
        private State Status { get { return (State)status_; } }

        /// <summary>
        /// ディスパッチャー。
        /// </summary>
        private Dispatcher dispatcher_;
        /// <summary>
        /// ディスパッチャーを取得する。
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// オブジェクトがDispose中、またはDispose済みならスローする。
        /// </exception>
        public Dispatcher Dispatcher
        {
            get
            {
                ThrowIfDisposingOrDisposed();
                return dispatcher_;
            }
        }

        #endregion  // プロパティ


        #region IDisposable実装

        /// <summary>
        /// リソースを破棄する。
        /// </summary>
        public void Dispose()
        {
            var prevStatus = (State)Interlocked.CompareExchange(ref status_, (int)State.Running, (int)State.Disposing);
            if (prevStatus != State.Running)
            {
                return;
            }

            dispatcher_.BeginInvokeShutdown(DispatcherPriority.Normal);
            loopThread_.Join();

            Interlocked.Exchange(ref status_, (int)State.Disposed);
        }

        #endregion  // IDisposable実装


        #region メソッド

        /// <summary>
        /// ワーカースレッドを起動して、スレッド上でイベントループを開始する。
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// オブジェクトがDisposeされているときにスローする。
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// イベントループが既に開始しているときにスローする。
        /// </exception>
        public void Run()
        {
            ThrowIfDisposingOrDisposed();

            var prevStatus = (State)Interlocked.CompareExchange(ref status_, (int)State.Running, (int)State.NotRunning);
            if (prevStatus != State.NotRunning)
            {
                throw new InvalidOperationException("イベントループが既に開始している");
            }

            loopThread_ = new Thread(Loop);
            using (var getDispatcherEvent = new AutoResetEvent(false))
            {
                loopThread_.Start(getDispatcherEvent);
                getDispatcherEvent.WaitOne();
            }
        }

        /// <summary>
        /// ワーカースレッドにイベントループ処理。
        /// </summary>
        /// <param name="state">
        /// <see cref="Run"/>メソッド実行スレッドに、<see cref="Dispatcher"/>を通知するためのイベント。
        /// </param>
        private void Loop(object state)
        {
            var getDispatcherEvent = state as AutoResetEvent;
            dispatcher_ = Dispatcher.CurrentDispatcher;
            getDispatcherEvent.Set();

            Dispatcher.Run();
        }

        /// <summary>
        /// オブジェクトがDispose中、またはDispose済みなら<see cref="ObjectDisposedException"/>をスローする。
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// オブジェクトがDispose中、またはDispose済みならスローする。
        /// </exception>
        private void ThrowIfDisposingOrDisposed()
        {
            var status = Status;
            switch (status)
            {
                case State.Disposing:
                case State.Disposed:
                    throw new ObjectDisposedException(GetType().FullName);

                default:
                    break;
            }
        }

        #endregion  // メソッド
    }
}
