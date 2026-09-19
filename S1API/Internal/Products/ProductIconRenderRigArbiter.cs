using System.Collections.Generic;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Serializes queued captures that share the native item-icon render rig.
    /// </summary>
    internal static class ProductIconRenderRigArbiter
    {
        private static readonly object Gate = new object();
        private static readonly Queue<CaptureLease> Pending =
            new Queue<CaptureLease>();
        private static CaptureLease? _owner;

        internal sealed class CaptureLease
        {
            internal bool Cancelled { get; set; }
        }

        internal static CaptureLease Enqueue()
        {
            lock (Gate)
            {
                var lease = new CaptureLease();
                Pending.Enqueue(lease);
                return lease;
            }
        }

        internal static bool TryAcquire(CaptureLease lease)
        {
            lock (Gate)
            {
                if (lease.Cancelled)
                    return false;
                if (ReferenceEquals(_owner, lease))
                    return true;
                if (_owner != null)
                    return false;

                RemoveCancelledHead();
                if (Pending.Count == 0 ||
                    !ReferenceEquals(Pending.Peek(), lease))
                {
                    return false;
                }

                _owner = Pending.Dequeue();
                return true;
            }
        }

        internal static void Release(CaptureLease lease)
        {
            lock (Gate)
            {
                if (ReferenceEquals(_owner, lease))
                    _owner = null;
                else
                    lease.Cancelled = true;

                RemoveCancelledHead();
            }
        }

        internal static void Cancel(CaptureLease lease)
        {
            lock (Gate)
            {
                lease.Cancelled = true;
                if (ReferenceEquals(_owner, lease))
                    _owner = null;
                RemoveCancelledHead();
            }
        }

        internal static void ResetForTesting()
        {
            lock (Gate)
            {
                Pending.Clear();
                _owner = null;
            }
        }

        private static void RemoveCancelledHead()
        {
            while (Pending.Count != 0 && Pending.Peek().Cancelled)
                Pending.Dequeue();
        }
    }
}
