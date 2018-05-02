using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using System.Collections.Generic;
using Xamarin.Forms;

namespace SecurityCameraClient
{
    public partial class MainPage : ContentPage
	{
        const string generateSasFunctionUri = "https://xxxxxxxxxx.azurewebsites.net/api/SasToken?code=xxxxxxxxxx";
        const string blobServiceEndpoint = "https://xxxxxxxxxx.blob.core.windows.net/";
        const string containerName = "xxxxxxxxxx";

        public MainPage()
        {
            InitializeComponent();

            // Read,List権限を付与したContainerのSASを取得
            var containerSasToken = getSasToken(generateSasFunctionUri, containerName, null, "Read,List");

            // Container内のBlob一覧を画面のListViewにセット
            CreateListViewAsync(blobServiceEndpoint + containerName, containerSasToken).GetAwaiter().GetResult();
        }

        // ContainerまたはBlobのSASTokenを取得する
        public static string getSasToken(String sasTokenUri, String container, String blobName, String permissions)
        {
            WebClient webClient = new WebClient();
            webClient.Headers[HttpRequestHeader.ContentType] = "application/json;charset=UTF-8";
            webClient.Headers[HttpRequestHeader.Accept] = "application/json";
            webClient.Encoding = Encoding.UTF8;

            // 問い合わせ内容をJSONオブジェクトに設定する
            var data = new RequestJsonModel();
            data.containerName = container;
            data.blobName = blobName;
            data.permission = permissions;

            // JSONをシリアライズしPOSTリクエストでFunctionに送信
            var reqData = JsonConvert.SerializeObject(data);
            return webClient.UploadString(new Uri(sasTokenUri), reqData);
        }

        // Container内のBlob一覧を画面のListViewにセット
        private async Task CreateListViewAsync(string uri, string sas)
        {
            // Containerへ接続しBlob一覧を取得する
            CloudBlobContainer container = new CloudBlobContainer(new Uri(uri), new StorageCredentials(sas));
            BlobContinuationToken blobToken = null;
            List<IListBlobItem> results = new List<IListBlobItem>();
            do
            {
                // 一度の問い合わせで返却されるリストは5,000件まで
                BlobResultSegment blobList = await container.ListBlobsSegmentedAsync(blobToken).ConfigureAwait(false);
                blobToken = blobList.ContinuationToken;
                results.AddRange(blobList.Results);
            // 継続Tokenがnullになったらリスト終了
            } while (blobToken != null);

            // Blob一覧をListViewにセット
            var blobUriList = new List<string>();
            foreach (IListBlobItem item in results) { blobUriList.Add(item.Uri.ToString()); }
            this.BlobList.ItemsSource = blobUriList;
        }

        // 画面のBlobリストが選択されたら
        private void OnItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var uri = e.SelectedItem.ToString();
            var fileName = Path.GetFileName(e.SelectedItem.ToString());
        
            // Read権限を付与したBlobのSASを取得
            var blobSasToken = getSasToken(generateSasFunctionUri, containerName, fileName, "Read");

            // SAS付きのURLでblobを開き画面のImageに表示
            this.SelectedImage.Source = uri + blobSasToken;
        }
    }
}