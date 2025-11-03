using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.ETL.Pipelines
{
    public abstract class PipelineDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<PipelineStage> Stages { get; set; }
        public virtual Task ExecuteAsync()
        {
            foreach (var stage in Stages)
            {
                stage.ExecuteAsync();
            }
            return Task.CompletedTask;
        }
    }
}
