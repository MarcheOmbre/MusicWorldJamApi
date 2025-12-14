using Microsoft.EntityFrameworkCore;
using WorldMusicJam.Models;
using User = WorldMusicJam.Models.User;

namespace WorldMusicJam.Contexts;

public class DataContextEntityFramework(IConfiguration configuration) : DbContext
{
    private readonly string publicSchema = "PublicSchema";
    private readonly string authentificationSchema = "AuthentificationSchema";
    
    
  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured) 
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("defaultConnection"), options => options.EnableRetryOnFailure());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>().ToTable("Users", publicSchema).HasKey(user => user.Id);
        modelBuilder.Entity<AuthentificationUser>().ToTable("Users", authentificationSchema).HasKey(user => user.Id);
        modelBuilder.Entity<Group>().ToTable("Groups", publicSchema).HasKey(group => group.Id);
        modelBuilder.Entity<Music>().ToTable("Musics", publicSchema).HasKey(music => music.Id);
        modelBuilder.Entity<Jam>().ToTable("Jams", publicSchema).HasKey(jam => jam.Id);
        modelBuilder.Entity<UserGroupJoin>().ToTable("UserGroupJoin", publicSchema).HasKey(userGroupJoin => userGroupJoin.Id);
        modelBuilder.Entity<GroupMusicJoin>().ToTable("GroupMusicJoin", publicSchema).HasKey(groupMusicJoin => groupMusicJoin.Id);
        modelBuilder.Entity<JamGroupJoin>().ToTable("JamGroupJoin", publicSchema).HasKey(jamGroupJoin => jamGroupJoin.Id);
        modelBuilder.Entity<JamMusicJoin>().ToTable("JamMusicJoin", publicSchema).HasKey(jamMusicJoin => jamMusicJoin.Id);
        modelBuilder.Entity<GroupInvitation>().ToTable("GroupInvitations", publicSchema).HasKey(groupInvitation => groupInvitation.Id);
        modelBuilder.Entity<Notation>().ToTable("Notations", publicSchema).HasKey(notation => notation.Id);
        modelBuilder.Entity<MusicComment>().ToTable("MusicComments", publicSchema).HasKey(musicComment => musicComment.Id);
        modelBuilder.Entity<GroupComment>().ToTable("GroupComments", publicSchema).HasKey(groupComment => groupComment.Id);
    }
}