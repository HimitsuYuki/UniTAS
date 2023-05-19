using UnityEngine;

namespace UniTAS.Plugin.Services.UnityAsyncOperationTracker;

public interface IAssetBundleRequestTracker
{
    void NewAssetBundleRequest(AsyncOperation asyncOperation, Object assetBundleRequest);
    void NewAssetBundleRequestMultiple(AsyncOperation asyncOperation, Object[] assetBundleRequestArray);

    object GetAssetBundleRequest(AsyncOperation asyncOperation);
    object GetAssetBundleRequestMultiple(AsyncOperation asyncOperation);
}