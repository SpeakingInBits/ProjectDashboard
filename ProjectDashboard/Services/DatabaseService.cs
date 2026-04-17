using ProjectDashboard.Models;
using SQLite;

namespace ProjectDashboard.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _database;

    private async Task InitAsync()
    {
        if (_database is not null)
            return;

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "projects.db3");
        _database = new SQLiteAsyncConnection(dbPath);
        await _database.CreateTableAsync<GitHubProject>();
    }

    public async Task<List<GitHubProject>> GetProjectsAsync()
    {
        await InitAsync();
        return await _database!.Table<GitHubProject>().ToListAsync();
    }

    public async Task<int> SaveProjectAsync(GitHubProject project)
    {
        await InitAsync();
        if (project.Id != 0)
            return await _database!.UpdateAsync(project);
        return await _database!.InsertAsync(project);
    }

    public async Task<int> DeleteProjectAsync(GitHubProject project)
    {
        await InitAsync();
        return await _database!.DeleteAsync(project);
    }
}
