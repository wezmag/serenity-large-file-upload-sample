using Serenity.Services;

namespace SereneLargeFileUpload.Northwind
{
    public class OrderListRequest : ListRequest
    {
        public int? ProductID { get; set; }
    }
}