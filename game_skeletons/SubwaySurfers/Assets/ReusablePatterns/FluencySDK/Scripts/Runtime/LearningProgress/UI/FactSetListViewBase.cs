using System.Collections.Generic;
using ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.Models;
using UnityEngine;
using UnityEngine.Pool;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.UI
{
    /// <summary>
    /// Abstract base class for fact set list views that provides common functionality
    /// including object pooling, container management, and data lifecycle management
    /// </summary>
    /// <typeparam name="TRow">The type of row item that implements IFactSetRowItem</typeparam>
    /// <typeparam name="TData">The type of data that rows display</typeparam>
    public abstract class FactSetListViewBase<TRow, TData> : MonoBehaviour 
        where TRow : MonoBehaviour, IFactSetRowItem<TData>
    {
        [Header("Common UI References")]
        [SerializeField] protected Transform rowContainer;
        [SerializeField] protected TRow rowPrefab;
        [SerializeField] protected GameObject noDataMessage;

        private ObjectPool<TRow> _rowPool;
        protected readonly List<TRow> _activeRows = new();
        private bool _initialized = false;

        protected virtual void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            InitializePool();
            OnInitialize();
        }

        /// <summary>
        /// Override this to perform additional initialization logic
        /// </summary>
        protected virtual void OnInitialize() { }

        private void InitializePool()
        {
            _rowPool = new ObjectPool<TRow>(
                createFunc: CreateRow,
                actionOnGet: row => {
                    row.GameObject.SetActive(true);
                    OnRowActivated(row);
                },
                actionOnRelease: row => {
                    OnRowDeactivated(row);
                    row.GameObject.SetActive(false);
                },
                actionOnDestroy: DestroyRow
            );
        }

        private TRow CreateRow()
        {
            var row = Instantiate(rowPrefab, rowContainer);
            OnRowCreated(row);
            return row;
        }

        private void DestroyRow(TRow row)
        {
            if (row != null)
            {
                OnRowDestroyed(row);
                Destroy(row.GameObject);
            }
        }

        /// <summary>
        /// Refreshes the list with new fact set progress data
        /// </summary>
        /// <param name="factSetProgresses">The source fact set progress data</param>
        public virtual void RefreshData(IList<FactSetProgress> factSetProgresses)
        {
            Initialize();
            ClearRows();

            if (factSetProgresses == null || factSetProgresses.Count == 0)
            {
                ShowNoDataMessage(true);
                OnDataEmpty();
                return;
            }

            var transformedData = TransformData(factSetProgresses);
            if (transformedData.Count == 0)
            {
                ShowNoDataMessage(true);
                OnDataEmpty();
                return;
            }

            ShowNoDataMessage(false);
            PopulateRows(transformedData);
            OnDataPopulated(transformedData);
        }

        /// <summary>
        /// Transforms raw FactSetProgress data into the format expected by rows
        /// </summary>
        /// <param name="source">Source fact set progress data</param>
        /// <returns>Transformed data for display</returns>
        protected abstract IReadOnlyList<TData> TransformData(IList<FactSetProgress> source);

        private void PopulateRows(IReadOnlyList<TData> data)
        {
            foreach (var item in data)
            {
                var row = _rowPool.Get();
                row.Transform.SetAsLastSibling();
                _activeRows.Add(row);
                
                row.Repaint(item);
                OnRowPopulated(row, item);
            }
        }

        private void ClearRows()
        {
            foreach (var row in _activeRows)
            {
                _rowPool.Release(row);
            }
            _activeRows.Clear();
            OnRowsCleared();
        }

        private void ShowNoDataMessage(bool show)
        {
            if (noDataMessage != null)
                noDataMessage.SetActive(show);
        }

        /// <summary>
        /// Gets the currently active rows (read-only)
        /// </summary>
        protected IReadOnlyList<TRow> ActiveRows => _activeRows;

        #region Virtual Event Hooks

        /// <summary>
        /// Called when a new row is created (not from pool)
        /// </summary>
        protected virtual void OnRowCreated(TRow row) { }

        /// <summary>
        /// Called when a row is activated from the pool
        /// </summary>
        protected virtual void OnRowActivated(TRow row) { }

        /// <summary>
        /// Called when a row is deactivated to the pool
        /// </summary>
        protected virtual void OnRowDeactivated(TRow row) { }

        /// <summary>
        /// Called when a row is permanently destroyed
        /// </summary>
        protected virtual void OnRowDestroyed(TRow row) { }

        /// <summary>
        /// Called after a row has been populated with data
        /// </summary>
        protected virtual void OnRowPopulated(TRow row, TData data) { }

        /// <summary>
        /// Called when all rows have been cleared
        /// </summary>
        protected virtual void OnRowsCleared() { }

        /// <summary>
        /// Called when data is successfully populated
        /// </summary>
        protected virtual void OnDataPopulated(IReadOnlyList<TData> data) { }

        /// <summary>
        /// Called when there is no data to display
        /// </summary>
        protected virtual void OnDataEmpty() { }

        #endregion

        protected virtual void OnDestroy()
        {
            // Clean up any remaining rows
            ClearRows();
        }
    }
} 