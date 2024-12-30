using AutoTag.Core.Files;
using AutoTag.Core.Test.Helpers;
using AutoTag.Core.TMDB;
using AutoTag.Core.TV;

namespace AutoTag.Core.Test.TV.TVProcessor;

public abstract class TVProcessorTestBase
{
    protected Core.TV.TVProcessor GetInstance(ITMDBService? tmdb = null,
        IFileWriter? writer = null,
        ITVCache? cache = null,
        IUserInterface? ui = null,
        AutoTagConfig? config = null
    )
        => new(
            tmdb.OrDefaultMock(),
            writer.OrDefaultMock(),
            cache.OrDefaultMock(),
            ui.OrDefaultMock(),
            config.OrDefaultMock()
        );
}