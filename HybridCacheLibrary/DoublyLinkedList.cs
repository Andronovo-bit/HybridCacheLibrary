namespace HybridCacheLibrary
{
    internal class DoublyLinkedList<K, V>
    {
        private Node<K, V> _head;
        private Node<K, V> _tail;

        public DoublyLinkedList()
        {
            _head = new Node<K, V>();
            _tail = new Node<K, V>();
            _head.Next = _tail;
            _tail.Prev = _head;
        }

        public void AddFirst(Node<K, V> node)
        {
            lock (this)
            {
                node.Next = _head.Next;
                node.Prev = _head;
                _head.Next.Prev = node;
                _head.Next = node;
            }
        }

        public void Remove(Node<K, V> node)
        {
            lock (this)
            {
                node.Prev.Next = node.Next;
                node.Next.Prev = node.Prev;
            }
        }

        public Node<K, V> RemoveLast()
        {
            lock (this)
            {
                if (IsEmpty())
                {
                    return null;
                }

                var node = _tail.Prev;
                Remove(node);
                return node;
            }
        }

        public bool IsEmpty()
        {
            return _head.Next == _tail;
        }
    }
}
