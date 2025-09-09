using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Pipelines
{
    public class BatchResult
    {
        public string Id = $"batch_{DateTime.Now.ToString("dd-MM-yyyy")}";
        public string Schema { get; set; } = "";
        public string Status { get; set; } = "incomplete";
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }
        public int BatchSize { get; set; }
        public object Data { get; set; } = new();
        public void MarkAsComplete(object data)
        {
            this.Data = data;
            this.FinishTime = DateTime.Now;
            this.Status = "complete";
        }
        public void MarkAsIncomplete(object data)
        {
            this.Data = data;
            this.FinishTime = DateTime.Now;
        }
    }
}
