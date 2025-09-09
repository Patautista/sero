using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.ETL.Pipelines
{
    public abstract class PipelineStage
    {
        PipelineDefinition Parent;
        public string Name { get; set; }
        public string Schema { get; set; }
        public abstract Task ExecuteAsync();
    }
}
