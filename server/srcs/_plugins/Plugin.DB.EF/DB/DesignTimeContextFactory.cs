// WingsEmu
// 
// Developed by NosWings Team

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Plugin.Database.DB
{
    public class DesignTimeContextFactory : IDesignTimeDbContextFactory<GameContext>
    {
        public GameContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<GameContext>();

            optionsBuilder.UseNpgsql(new DatabaseConfiguration().ToString());
            return new GameContext(optionsBuilder.Options);
        }
    }
}