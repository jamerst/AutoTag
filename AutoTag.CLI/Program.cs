var app = new CommandApp<AutoTag.CLI.RootCommand>()
    .WithDescription("Automatically tag and rename media files");

return await app.RunAsync(args);