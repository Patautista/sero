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
            Directory.CreateDirectory(_basePath);
        }
        public T GetData<T>()
        {
            var json = File.ReadAllText(_dataPath);
            return JsonSerializer.Deserialize<T>(json);
        }
        public int BatchCount()
        {
            var count = Directory.EnumerateFiles(_basePath, "*.json", SearchOption.TopDirectoryOnly)?.Count() ?? 0;
            return count;
        }
        public void SaveBatch(BatchResult result)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
            var json = JsonSerializer.Serialize(result.Data, options);
            File.WriteAllText($"{Path.Combine(_basePath, result.Id)}.json", json);
        }
        public string GetStagePath(PipelineDefinition pipeline, int index) {
            return $"{_basePath}/{index} {pipeline.Stages[index].Name}";
        }
    }
}
