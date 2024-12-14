using AutoTag.Core.Files;
using AutoTag.Core.Test.Helpers;
using AutoTag.Core.TMDB;

namespace AutoTag.Core.Test.TV.TVProcessor;

public abstract class TVProcessorTestBase
{
    protected Core.TV.TVProcessor GetInstance(ITMDBService? tmdb = null,
        IFileWriter? writer = null,
        IUserInterface? ui = null,
        AutoTagConfig? config = null
    )
        => new(
            tmdb.OrDefaultMock(),
            writer.OrDefaultMock(),
            ui.OrDefaultMock(),
            config.OrDefaultMock()
        );
}