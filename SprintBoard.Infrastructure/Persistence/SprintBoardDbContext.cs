using Microsoft.EntityFrameworkCore;
using SprintBoard.Domain.Entities;

namespace SprintBoard.Infrastructure.Persistence
{
    /// <summary>
    /// Represents the Entity Framework Core database context for SprintBoard.
    /// </summary>
    public sealed class SprintBoardDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SprintBoardDbContext"/> class.
        /// </summary>
        /// <param name="options">
        /// Entity Framework Core options containing the provider, connection, and context configuration.
        /// </param>
        public SprintBoardDbContext(DbContextOptions<SprintBoardDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets the users database set.
        /// </summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>
        /// Gets the boards database set.
        /// </summary>
        public DbSet<Board> Boards => Set<Board>();

        /// <summary>
        /// Gets the board memberships database set.
        /// </summary>
        public DbSet<BoardMember> BoardMembers => Set<BoardMember>();

        /// <summary>
        /// Gets the cards database set.
        /// </summary>
        public DbSet<Card> Cards => Set<Card>();

        /// <summary>
        /// Gets the board invitations database set.
        /// </summary>
        public DbSet<BoardInvitation> BoardInvitations => Set<BoardInvitation>();

        /// <summary>
        /// Gets the card checklist tasks database set.
        /// </summary>
        public DbSet<CardTask> CardTasks => Set<CardTask>();

        /// <summary>
        /// Configures entity mappings, relationships, indexes, and persistence constraints for the SprintBoard model.
        /// </summary>
        /// <param name="modelBuilder">
        /// Entity Framework Core model builder used to configure the database model.
        /// </param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureUser(modelBuilder);
            ConfigureBoard(modelBuilder);
            ConfigureBoardMember(modelBuilder);
            ConfigureCard(modelBuilder);
            ConfigureCardTask(modelBuilder);
            ConfigureBoardInvitation(modelBuilder);
        }

        /// <summary>
        /// Configures persistence rules, indexes, and constraints for users.
        /// </summary>
        /// <param name="modelBuilder">
        /// The Entity Framework Core model builder being configured.
        /// </param>
        private static void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(user => user.Id);

                entity.Property(user => user.Email)
                    .IsRequired()
                    .HasMaxLength(320);

                entity.Property(user => user.PasswordHash)
                    .IsRequired();

                entity.Property(user => user.FullName)
                    .IsRequired();

                entity.Property(user => user.ProfileImageUrl);

                entity.HasIndex(user => user.Email)
                    .IsUnique();

                entity.HasIndex(user => user.Username)
                    .IsUnique();
            });
        }

        /// <summary>
        /// Configures persistence rules and relationships for boards.
        /// </summary>
        /// <param name="modelBuilder">
        /// The Entity Framework Core model builder being configured.
        /// </param>
        private static void ConfigureBoard(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Board>(entity =>
            {
                entity.HasKey(board => board.Id);

                entity.Property(board => board.Name)
                    .IsRequired()
                    .HasMaxLength(120);

                entity.HasOne(board => board.Owner)
                    .WithMany()
                    .HasForeignKey(board => board.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(board => board.Members)
                    .WithOne(member => member.Board)
                    .HasForeignKey(member => member.BoardId);
            });
        }

        /// <summary>
        /// Configures persistence rules, role conversion, uniqueness, and relationships for board memberships.
        /// </summary>
        /// <param name="modelBuilder">
        /// The Entity Framework Core model builder being configured.
        /// </param>
        private static void ConfigureBoardMember(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BoardMember>(entity =>
            {
                entity.HasKey(member => member.Id);

                entity.Property(member => member.Role)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(member => member.JoinedAt)
                    .IsRequired();

                entity.HasIndex(member => new { member.BoardId, member.UserId })
                    .IsUnique();

                entity.HasOne(member => member.Board)
                    .WithMany(board => board.Members)
                    .HasForeignKey(member => member.BoardId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(member => member.User)
                    .WithMany(user => user.BoardMemberships)
                    .HasForeignKey(member => member.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        /// <summary>
        /// Configures persistence rules, relationships, and ordering indexes for cards.
        /// </summary>
        /// <param name="modelBuilder">
        /// The Entity Framework Core model builder being configured.
        /// </param>
        private static void ConfigureCard(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Card>(entity =>
            {
                entity.ToTable("Card");

                entity.HasKey(card => card.Id);

                entity.Property(card => card.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(card => card.Status)
                    .IsRequired();

                entity.Property(card => card.Position)
                    .IsRequired();

                entity.HasOne(card => card.Board)
                    .WithMany()
                    .HasForeignKey(card => card.BoardId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(card => new { card.BoardId, card.Status, card.Position });
            });
        }

        /// <summary>
        /// Configures persistence rules, relationships, and ordering indexes for checklist tasks.
        /// </summary>
        /// <param name="modelBuilder">
        /// The Entity Framework Core model builder being configured.
        /// </param>
        private static void ConfigureCardTask(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CardTask>(entity =>
            {
                entity.HasKey(cardTask => cardTask.Id);

                entity.Property(cardTask => cardTask.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(cardTask => cardTask.IsCompleted)
                    .IsRequired();

                entity.Property(cardTask => cardTask.Position)
                    .IsRequired();

                entity.Property(cardTask => cardTask.CreatedAt)
                    .IsRequired();

                entity.Property(cardTask => cardTask.UpdatedAt)
                    .IsRequired();

                entity.HasOne(cardTask => cardTask.Card)
                    .WithMany(card => card.Tasks)
                    .HasForeignKey(cardTask => cardTask.CardId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(cardTask => new { cardTask.CardId, cardTask.Position });
            });
        }

        /// <summary>
        /// Configures persistence rules, token uniqueness, and relationships for board invitations.
        /// </summary>
        /// <param name="modelBuilder">
        /// The Entity Framework Core model builder being configured.
        /// </param>
        private static void ConfigureBoardInvitation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BoardInvitation>(entity =>
            {
                entity.HasKey(invitation => invitation.Id);

                entity.Property(invitation => invitation.Email)
                    .IsRequired()
                    .HasMaxLength(320);

                entity.Property(invitation => invitation.Token)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(invitation => invitation.Status)
                    .IsRequired();

                entity.Property(invitation => invitation.ExpiresAt)
                    .IsRequired();

                entity.Property(invitation => invitation.CreatedAt)
                    .IsRequired();

                entity.HasIndex(invitation => invitation.Token)
                    .IsUnique();

                entity.HasOne(invitation => invitation.Board)
                    .WithMany()
                    .HasForeignKey(invitation => invitation.BoardId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(invitation => invitation.InvitedByUser)
                    .WithMany()
                    .HasForeignKey(invitation => invitation.InvitedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
