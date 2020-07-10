using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace autotag.Core {
    public interface IProcessor {
        Task<bool> process(String filePath, Action<String> setPath, Action<String> setStatus, Func<List<Tuple<String, int>>, int> selectResult, AutoTagConfig config);
    }
}