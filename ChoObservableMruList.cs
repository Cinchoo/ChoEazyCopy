using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;


namespace ChoEazyCopy
{
    
    public class ChoObservableMruList<T> : ObservableCollection<T>
    {

        #region Fields

        private readonly int _maxSize = -1;
        private readonly IEqualityComparer<T> _itemComparer = null;

        #endregion

        #region Constructors

        public ChoObservableMruList() : base()
        {

        }

        public ChoObservableMruList(IEnumerable<T> collection) : base(collection)
        {

        }

        public ChoObservableMruList(List<T> list) : base(list)
        {
            
        }

        public ChoObservableMruList(int maxSize, IEqualityComparer<T> itemComparer) : base()
        {
            _maxSize = maxSize;
            _itemComparer = itemComparer;
        }

        public ChoObservableMruList(IEnumerable<T> collection, int maxSize, IEqualityComparer<T> itemComparer)
            : base(collection)
        {
            _maxSize = maxSize;
            _itemComparer = itemComparer;
            RemoveOverflow();
        }

        public ChoObservableMruList(List<T> list, int maxSize, IEqualityComparer<T> itemComparer)
            : base(list)
        {
            _maxSize = maxSize;
            _itemComparer = itemComparer;
            RemoveOverflow();
        }

        #endregion
        
        #region Properties

        public int MaxSize
        {
            get { return _maxSize; }
        }
        
        #endregion

        #region Public Methods

        public new void Add(T item)
        {

            int indexOfMatch = this.IndexOf(item);
            if (indexOfMatch < 0)
            {
                base.Insert(0, item);
            }
            else
            {
                base.Move(indexOfMatch, 0);
            }

            RemoveOverflow();
            
        }

        public new bool Contains(T item)
        {               
            return this.Contains(item, _itemComparer);
        }

        public new int IndexOf(T item)
        {

            int indexOfMatch = -1;

            if (_itemComparer != null)
            {
                for (int idx = 0; idx < this.Count; idx++)
                {
                    if (_itemComparer.Equals(item, this[idx]))
                    {
                        indexOfMatch = idx;
                        break;
                    }
                }
            }
            else
            {
                indexOfMatch = base.IndexOf(item);
            }

            return indexOfMatch;

        }

        public new bool Remove(T item)
        {

            bool opResult = false;

            int targetIndex = this.IndexOf(item);
            if (targetIndex > -1)
            {
                this.RemoveAt(targetIndex);
                opResult = true;
            }

            return opResult;

        }
        
        #endregion

        #region Helper Methods

        private void RemoveOverflow()
        {
            
            if (this.MaxSize > 0)
            {
                while (this.Count > this.MaxSize)
                {
                    this.RemoveAt(this.Count - 1);
                }
            }

        }

        #endregion

    }

}
