using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;

namespace SubwaySurfers.UI.PreviewSystem
{
    /// <summary>
    /// Marker interface for objects that can be previewed
    /// </summary>
    public interface IPreviewable
    {
        string GetPreviewId();
    }
    public class PreviwableAssetAddress
    {
        public string AssetAddress { get; private set; }
        public AssetReference AddressableReference { get; private set; }

        public static PreviwableAssetAddress FromAssetReference(AssetReference assetReference)
        {
            return new PreviwableAssetAddress
            {
                AddressableReference = assetReference
            };
        }

        public static PreviwableAssetAddress FromAssetAddress(string assetAddress)
        {
            return new PreviwableAssetAddress
            {
                AssetAddress = assetAddress
            };
        }

        public bool HasValue()
        {
            return AddressableReference != null || !string.IsNullOrWhiteSpace(AssetAddress);
        }

        public override string ToString()
        {
            return AddressableReference != null ? AddressableReference.ToString() : AssetAddress;
        }
    }

    /// <summary>
    /// Generic interface for preview data wrappers
    /// </summary>
    /// <typeparam name="T">The type being previewed</typeparam>
    public interface IPreviewData<T> where T : class
    {
        T Data { get; }
        PreviwableAssetAddress AssetAddress { get; }
        TransformData? TransformData {get;}
    }

    /// <summary>
    /// Generic interface for preview controllers
    /// </summary>
    /// <typeparam name="TData">The type being previewed</typeparam>
    public interface IPreviewController<TPreviewData, TData> where TData : class
    where TPreviewData : IPreviewData<TData>
    {
        bool IsVisible { get; }
        bool IsLoading { get; }
        UniTask ShowPreviewAsync(TPreviewData previewData);
        void HidePreview();
        void SetAutoHide(bool enabled, float delay = 5f);
    }

    /// <summary>
    /// Generic interface for preview UI components
    /// </summary>
    /// <typeparam name="T">The type being previewed</typeparam>
    public interface IPreviewUI<TPreviewData, TData> where TData : class
        where TPreviewData : IPreviewData<TData>
    {
        bool IsVisible { get; }
        void Show();
        void Hide();
        void UpdateDisplay(TPreviewData previewData);
    }
}