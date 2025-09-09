using Business.ETL.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Pipelines
{
    public class FileDatalakeService
    {
        private string _basePath { get; set; }
        private string _dataPath { get; set; }
        public FileDatalakeService(string basePath, string dataPath)
        {
            _basePath = basePath;
            _dataPath = dataPath;
        }
        public void Configure(PipelineStage stage)
        {
            _basePath = Path.Combine(_basePath, stage.Name);
        }
        public T GetData<T>()
        {
            var json = File.ReadAllText(_dataPath);
            return JsonSerializer.Deserialize<T>(json);
        }
        public string GetStagePath(PipelineDefinition pipeline, int index) {
            return $"{_basePath}/{index} {pipeline.Stages[index].Name}";
        }
    }
}
