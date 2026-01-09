using Microsoft.EntityFrameworkCore;
using WorldMusicJam.Helpers;
using WorldMusicJam.Models;
using User = WorldMusicJam.Models.User;

namespace WorldMusicJam.Contexts;

public class DataContextEntityFramework(IConfiguration configuration) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("defaultConnection"),
                options => options.EnableRetryOnFailure());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().ToTable("Users", Constants.MainSchema).HasKey(user => user.Id);
        modelBuilder.Entity<AuthentificationUser>().ToTable("Users", Constants.AuthentificationSchema).HasKey(user => user.Id);
        modelBuilder.Entity<Group>().ToTable("Groups", Constants.MainSchema).HasKey(group => group.Id);
        modelBuilder.Entity<Music>().ToTable("Musics", Constants.MainSchema).HasKey(music => music.Id);
        modelBuilder.Entity<Jam>().ToTable("Jams", Constants.MainSchema).HasKey(jam => jam.Id);
        modelBuilder.Entity<UserGroupJoin>().ToTable("UserGroupJoin", Constants.MainSchema)
            .HasKey(userGroupJoin => userGroupJoin.Id);
        modelBuilder.Entity<GroupMusicJoin>().ToTable("GroupMusicJoin", Constants.MainSchema)
            .HasKey(groupMusicJoin => groupMusicJoin.Id);
        modelBuilder.Entity<JamGroupJoin>().ToTable("JamGroupJoin", Constants.MainSchema)
            .HasKey(jamGroupJoin => jamGroupJoin.Id);
        modelBuilder.Entity<JamMusicJoin>().ToTable("JamMusicJoin", Constants.MainSchema)
            .HasKey(jamMusicJoin => jamMusicJoin.Id);
        modelBuilder.Entity<GroupInvitation>().ToTable("GroupInvitations", Constants.MainSchema)
            .HasKey(groupInvitation => groupInvitation.Id);
        modelBuilder.Entity<Notation>().ToTable("Notations", Constants.MainSchema).HasKey(notation => notation.Id);
        modelBuilder.Entity<MusicComment>().ToTable("MusicComments", Constants.MainSchema)
            .HasKey(musicComment => musicComment.Id);
        modelBuilder.Entity<GroupComment>().ToTable("GroupComments", Constants.MainSchema)
            .HasKey(groupComment => groupComment.Id);
    }
}