using System.Runtime.InteropServices;
using static GHelper.UI.CompositionInterop;

namespace GHelper.UI
{
    // The compositor and DispatcherQueue belong to the WinForms message loop and are shared by its windows.
    internal sealed unsafe class WindowComposition
    {
        private const int SingleThreaded = 0;
        private const int MultiThreaded = 1;
        private const int CurrentThreadQueue = 2;
        private const int GetQueueForCurrentThread = FirstInspectableMethod;
        private const int ShutdownQueueAsync = FirstInspectableMethod + 1;
        private const int GetAsyncStatus = FirstInspectableMethod + 1;
        private const int AsyncStarted = 0;
        private const uint AllInput = 0x04ff;
        private const uint InputAvailable = 0x0004;
        private static readonly Guid QueueStaticsId = new("a96d83d7-9371-4517-9245-d0824ac12c74");
        private static readonly Guid AsyncInfoId = new("00000036-0000-0000-c000-000000000046");

        [ThreadStatic] private static WindowComposition? current;

        public static WindowComposition ForCurrentThread => current ??= new WindowComposition();

        public ComReference Compositor { get; }
        private readonly ComReference? queueController;

        private WindowComposition()
        {
            Check(RoInitialize(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA ? MultiThreaded : SingleThreaded));
            try
            {
                using var queueStatics = GetFactory("Windows.System.DispatcherQueue", QueueStaticsId);
                using var existingQueue = GetObject(queueStatics, GetQueueForCurrentThread);
                if (existingQueue.IsInvalid)
                {
                    var options = new QueueOptions { Size = sizeof(QueueOptions), ThreadType = CurrentThreadQueue };
                    Check(CreateDispatcherQueueController(options, out nint controller));
                    queueController = new(controller);
                }
                Compositor = Activate("Windows.UI.Composition.Compositor");
            }
            catch
            {
                CloseQueue();
                RoUninitialize();
                throw;
            }
        }

        public static void Shutdown()
        {
            if (current is not { } composition) return;
            composition.Compositor.Dispose();
            composition.CloseQueue();
            current = null;
            RoUninitialize();
        }

        private void CloseQueue()
        {
            if (queueController is null) return;
            try
            {
                using var shutdown = GetObject(queueController, ShutdownQueueAsync);
                using var operation = shutdown.Query(AsyncInfoId);
                nint getStatus = Method(operation.Pointer, GetAsyncStatus);
                // The current thread must keep dispatching messages while Windows shuts down its queue.
                long deadline = Environment.TickCount64 + 1000;
                int status;
                while (((delegate* unmanaged[Stdcall]<nint, int*, int>)getStatus)(operation.Pointer, &status) >= 0 &&
                    status == AsyncStarted && Environment.TickCount64 < deadline)
                {
                    Application.DoEvents();
                    MsgWaitForMultipleObjectsEx(0, 0, 15, AllInput, InputAvailable);
                }
            }
            catch (COMException e) { Logger.WriteLine($"Composition queue shutdown: {e.Message}"); }
            finally { queueController.Dispose(); }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct QueueOptions
        {
            public int Size;
            public int ThreadType;
            public int ApartmentType;
        }

        [DllImport("combase.dll")]
        private static extern int RoInitialize(int apartment);

        [DllImport("combase.dll")]
        private static extern void RoUninitialize();

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController(QueueOptions options, out nint controller);

        [DllImport("user32.dll")]
        private static extern uint MsgWaitForMultipleObjectsEx(uint count, nint handles, uint milliseconds, uint wakeMask, uint flags);
    }
}
