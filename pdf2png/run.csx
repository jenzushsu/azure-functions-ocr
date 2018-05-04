#r "Microsoft.WindowsAzure.Storage"
#r "System.Drawing"
#r "System.Web"
#r "System.Configuration"

using System;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
/* using Microsoft.WindowsAzure.Storage.Queue; */
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;

public static void Run(string inputQueueItem,  ICollector<string> outputQueueItem, TraceWriter log)
{
    log.Info($"C# Queue trigger function processed. Value passed: {inputQueueItem}");
    string upload_filename = inputQueueItem;
    string storage_account_key = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
    string container_name = "input";
    // string queue_name = "png-reference-queue";

    var cloud_storage_account = CloudStorageAccount.Parse(storage_account_key);
    var blob_account = cloud_storage_account.CreateCloudBlobClient();
    var blob_container = blob_account.GetContainerReference(container_name);

    // var queue_account = cloud_storage_account.CreateCloudQueueClient();
    // var queue = queue_account.GetQueueReference(queue_name);

    CloudBlockBlob source_blob = blob_container.GetBlockBlobReference(upload_filename);

    if (!source_blob.Exists())
    {
        log.Info($"Source Blob does not exist.");
        return;
    }

    // queue.CreateIfNotExists();

    string temp_filepath = @"D:\home\data\Functions\sampledata";
    string file_path = string.Format(@"{0}\{1}", temp_filepath, upload_filename);

    source_blob.DownloadToFile(file_path, FileMode.Create);

    string file_name = Path.GetFileNameWithoutExtension(file_path);
    string png_filepath = string.Format(@"{0}\{1}.png", temp_filepath, file_name);

    // Delete PNG file if already exists
    if (File.Exists(png_filepath))
    {
        File.Delete(png_filepath);
    }

    // Use Ghostscript to convert from pdf to png
    string converted_output_name = "output";
    var converted_output_container = blob_account.GetContainerReference(converted_output_name);

    int desired_x_dpi = 96;
    int desired_y_dpi = 96;

    string input_pdf_path = file_path;
    string output_path = Path.GetDirectoryName(png_filepath);

    GhostscriptVersionInfo gvi = new GhostscriptVersionInfo(@"D:\home\data\Functions\packages\nuget\ghostscript.net\1.2.1\lib\net40\gsdll64.dll");

    using (GhostscriptRasterizer _rasterizer = new GhostscriptRasterizer())
    {
        _rasterizer.Open(input_pdf_path, gvi, true);

        for (int page_num = 1; page_num <= _rasterizer.PageCount; page_num++)
        {
            string page_filepath = Path.Combine(output_path, file_name + "-Page-" + page_num.ToString() + ".png");

            Image img = _rasterizer.GetPage(desired_x_dpi, desired_y_dpi, page_num);
            img.Save(page_filepath, ImageFormat.Png);

            string png_file_name = Path.GetFileName(page_filepath);

            // CloudQueueMessage message = new CloudQueueMessage(png_file_name);
            // queue.AddMessage(message);
            outputQueueItem.Add(png_file_name);

            CloudBlockBlob target_blob = converted_output_container.GetBlockBlobReference(png_file_name);

            using (FileStream file_stream = File.OpenRead(page_filepath))
            {
                target_blob.UploadFromStream(file_stream);
            }

            File.Delete(page_filepath);
        }
    }

    File.Delete(file_path);
}
