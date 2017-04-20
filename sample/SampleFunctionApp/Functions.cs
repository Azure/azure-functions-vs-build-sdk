using System;
using System.Collections.Generic;
using System.IO;
using ImageResizer;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus.Messaging;

namespace SDKFuncs
{
    public static class Functions
    {
        [FunctionName("ResizeImage")]
        public static void RunResizeImage(
            [BlobTrigger("sample-images/{name}")] Stream image, // input blob, large size
            [Blob("sample-images-sm/{name}", FileAccess.Write)] Stream imageSmall,
            [Blob("sample-images-md/{name}", FileAccess.Write)] Stream imageMedium)  // output blobs
        {
            var imageBuilder = ImageResizer.ImageBuilder.Current;
            var size = imageDimensionsTable[ImageSize.Small];

            imageBuilder.Build(
                image, imageSmall,
                new ResizeSettings(size.Item1, size.Item2, FitMode.Max, null), false);

            image.Position = 0;
            size = imageDimensionsTable[ImageSize.Medium];

            imageBuilder.Build(
                image, imageMedium,
                new ResizeSettings(size.Item1, size.Item2, FitMode.Max, null), false);
        }

        public enum ImageSize
        {
            ExtraSmall, Small, Medium
        }

        private static Dictionary<ImageSize, Tuple<int, int>> imageDimensionsTable = new Dictionary<ImageSize, Tuple<int, int>>()
        {
            { ImageSize.ExtraSmall, Tuple.Create(320, 200) },
            { ImageSize.Small,      Tuple.Create(640, 400) },
            { ImageSize.Medium,     Tuple.Create(800, 600) }
        };


        [FunctionName("ServiceBus")]
        public static void RunServiceBus([ServiceBusTrigger("queue", "sub", AccessRights.Manage)] string message)
        {

        }
    }
}
