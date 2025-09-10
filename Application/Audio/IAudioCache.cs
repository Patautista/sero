using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Audio
{
    public interface IAudioCache
    {
        Task<byte[]?> GetAsync(string key);
        Task SetAsync(string key, byte[] data);
    }
}
