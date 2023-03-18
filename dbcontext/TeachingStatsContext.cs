using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DBContext;

#pragma warning disable CS8618 
public partial class TeachingStatsContext : DbContext
{
    public TeachingStatsContext()
    {
    }

    public TeachingStatsContext(DbContextOptions<TeachingStatsContext> options) : base(options)
    {
    }

    public virtual DbSet<AccountEmailaddress> AccountEmailaddresses { get; set; }

    public virtual DbSet<AccountEmailconfirmation> AccountEmailconfirmations { get; set; }

    public virtual DbSet<Answer> Answers { get; set; }

    public virtual DbSet<AnswerAll> AnswerAlls { get; set; }

    public virtual DbSet<AnswerCfMp> AnswerCfMps { get; set; }

    public virtual DbSet<AnswerDeptAdm> AnswerDeptAdms { get; set; }

    public virtual DbSet<AnswerDeptAdmMp> AnswerDeptAdmMps { get; set; }

    public virtual DbSet<AnswerDeptInf> AnswerDeptInfs { get; set; }

    public virtual DbSet<AnswerDeptInfMp> AnswerDeptInfMps { get; set; }

    public virtual DbSet<AuthGroup> AuthGroups { get; set; }

    public virtual DbSet<AuthGroupPermission> AuthGroupPermissions { get; set; }

    public virtual DbSet<AuthPermission> AuthPermissions { get; set; }

    public virtual DbSet<AuthUser> AuthUsers { get; set; }

    public virtual DbSet<AuthUserGroup> AuthUserGroups { get; set; }

    public virtual DbSet<AuthUserUserPermission> AuthUserUserPermissions { get; set; }

    public virtual DbSet<Degree> Degrees { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<DjangoAdminLog> DjangoAdminLogs { get; set; }

    public virtual DbSet<DjangoContentType> DjangoContentTypes { get; set; }

    public virtual DbSet<DjangoMigration> DjangoMigrations { get; set; }

    public virtual DbSet<DjangoSession> DjangoSessions { get; set; }

    public virtual DbSet<DjangoSite> DjangoSites { get; set; }

    public virtual DbSet<FormsAnswer> FormsAnswers { get; set; }

    public virtual DbSet<FormsEvaluation> FormsEvaluations { get; set; }

    public virtual DbSet<FormsParticipation> FormsParticipations { get; set; }

    public virtual DbSet<FormsStudent> FormsStudents { get; set; }

    public virtual DbSet<FormsSubject> FormsSubjects { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<Level> Levels { get; set; }

    public virtual DbSet<Participation> Participations { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<SocialaccountSocialaccount> SocialaccountSocialaccounts { get; set; }

    public virtual DbSet<SocialaccountSocialapp> SocialaccountSocialapps { get; set; }

    public virtual DbSet<SocialaccountSocialappSite> SocialaccountSocialappSites { get; set; }

    public virtual DbSet<SocialaccountSocialtoken> SocialaccountSocialtokens { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<SubjectTrainerGroup> SubjectTrainerGroups { get; set; }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<Trainer> Trainers { get; set; }

    public virtual DbSet<Type> Types { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder){
        var settings = Utils.Settings;  
        if(settings == null || settings.TeachingStats == null) throw new IncorrectSettingsException();      
        
        optionsBuilder.UseNpgsql("Host={settings.TeachingStats.Host};Database=teaching-stats;Username={settings.TeachingStats.Username};Password={settings.TeachingStats.Password}");    
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEmailaddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("account_emailaddress_pkey");

            entity.ToTable("account_emailaddress");

            entity.HasIndex(e => e.Email, "account_emailaddress_email_03be32b2_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Email, "account_emailaddress_email_key").IsUnique();

            entity.HasIndex(e => e.UserId, "account_emailaddress_user_id_2c513194");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .HasColumnName("email");
            entity.Property(e => e.Primary).HasColumnName("primary");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Verified).HasColumnName("verified");

            entity.HasOne(d => d.User).WithMany(p => p.AccountEmailaddresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("account_emailaddress_user_id_2c513194_fk_auth_user_id");
        });

        modelBuilder.Entity<AccountEmailconfirmation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("account_emailconfirmation_pkey");

            entity.ToTable("account_emailconfirmation");

            entity.HasIndex(e => e.EmailAddressId, "account_emailconfirmation_email_address_id_5b7f8c58");

            entity.HasIndex(e => e.Key, "account_emailconfirmation_key_f43612bd_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Key, "account_emailconfirmation_key_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Created).HasColumnName("created");
            entity.Property(e => e.EmailAddressId).HasColumnName("email_address_id");
            entity.Property(e => e.Key)
                .HasMaxLength(64)
                .HasColumnName("key");
            entity.Property(e => e.Sent).HasColumnName("sent");

            entity.HasOne(d => d.EmailAddress).WithMany(p => p.AccountEmailconfirmations)
                .HasForeignKey(d => d.EmailAddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("account_emailconfirm_email_address_id_5b7f8c58_fk_account_e");
        });

        modelBuilder.Entity<Answer>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("answer", "reports");

            entity.Property(e => e.Degree)
                .HasMaxLength(4)
                .HasColumnName("degree");
            entity.Property(e => e.Department)
                .HasMaxLength(75)
                .HasColumnName("department");
            entity.Property(e => e.EvaluationId).HasColumnName("evaluation_id");
            entity.Property(e => e.Group)
                .HasMaxLength(11)
                .HasColumnName("group");
            entity.Property(e => e.Level)
                .HasMaxLength(3)
                .HasColumnName("level");
            entity.Property(e => e.QuestionSort).HasColumnName("question_sort");
            entity.Property(e => e.QuestionStatement).HasColumnName("question_statement");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(25)
                .HasColumnName("question_type");
            entity.Property(e => e.SubjectCode)
                .HasMaxLength(10)
                .HasColumnName("subject_code");
            entity.Property(e => e.SubjectName)
                .HasMaxLength(75)
                .HasColumnName("subject_name");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.Topic)
                .HasMaxLength(25)
                .HasColumnName("topic");
            entity.Property(e => e.Trainer)
                .HasMaxLength(75)
                .HasColumnName("trainer");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<AnswerAll>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("answer_all", "reports");

            entity.Property(e => e.Degree)
                .HasMaxLength(4)
                .HasColumnName("degree");
            entity.Property(e => e.Department)
                .HasMaxLength(75)
                .HasColumnName("department");
            entity.Property(e => e.EvaluationId).HasColumnName("evaluation_id");
            entity.Property(e => e.Group)
                .HasMaxLength(11)
                .HasColumnName("group");
            entity.Property(e => e.Level)
                .HasMaxLength(3)
                .HasColumnName("level");
            entity.Property(e => e.QuestionSort).HasColumnName("question_sort");
            entity.Property(e => e.QuestionStatement).HasColumnName("question_statement");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(25)
                .HasColumnName("question_type");
            entity.Property(e => e.SubjectCode)
                .HasMaxLength(10)
                .HasColumnName("subject_code");
            entity.Property(e => e.SubjectName)
                .HasMaxLength(75)
                .HasColumnName("subject_name");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.Topic)
                .HasMaxLength(25)
                .HasColumnName("topic");
            entity.Property(e => e.Trainer)
                .HasMaxLength(75)
                .HasColumnName("trainer");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<AnswerCfMp>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("answer_cf_mp", "reports");

            entity.Property(e => e.Degree)
                .HasMaxLength(4)
                .HasColumnName("degree");
            entity.Property(e => e.Department)
                .HasMaxLength(75)
                .HasColumnName("department");
            entity.Property(e => e.EvaluationId).HasColumnName("evaluation_id");
            entity.Property(e => e.Group)
                .HasMaxLength(11)
                .HasColumnName("group");
            entity.Property(e => e.Level)
                .HasMaxLength(3)
                .HasColumnName("level");
            entity.Property(e => e.QuestionSort).HasColumnName("question_sort");
            entity.Property(e => e.QuestionStatement).HasColumnName("question_statement");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(25)
                .HasColumnName("question_type");
            entity.Property(e => e.SubjectCode)
                .HasMaxLength(10)
                .HasColumnName("subject_code");
            entity.Property(e => e.SubjectName)
                .HasMaxLength(75)
                .HasColumnName("subject_name");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.Topic)
                .HasMaxLength(25)
                .HasColumnName("topic");
            entity.Property(e => e.Trainer)
                .HasMaxLength(75)
                .HasColumnName("trainer");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<AnswerDeptAdm>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("answer_dept_adm", "reports");

            entity.Property(e => e.Degree)
                .HasMaxLength(4)
                .HasColumnName("degree");
            entity.Property(e => e.Department)
                .HasMaxLength(75)
                .HasColumnName("department");
            entity.Property(e => e.EvaluationId).HasColumnName("evaluation_id");
            entity.Property(e => e.Group)
                .HasMaxLength(11)
                .HasColumnName("group");
            entity.Property(e => e.Level)
                .HasMaxLength(3)
                .HasColumnName("level");
            entity.Property(e => e.QuestionSort).HasColumnName("question_sort");
            entity.Property(e => e.QuestionStatement).HasColumnName("question_statement");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(25)
                .HasColumnName("question_type");
            entity.Property(e => e.SubjectCode)
                .HasMaxLength(10)
                .HasColumnName("subject_code");
            entity.Property(e => e.SubjectName)
                .HasMaxLength(75)
                .HasColumnName("subject_name");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.Topic)
                .HasMaxLength(25)
                .HasColumnName("topic");
            entity.Property(e => e.Trainer)
                .HasMaxLength(75)
                .HasColumnName("trainer");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<AnswerDeptAdmMp>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("answer_dept_adm_mp", "reports");

            entity.Property(e => e.Degree)
                .HasMaxLength(4)
                .HasColumnName("degree");
            entity.Property(e => e.Department)
                .HasMaxLength(75)
                .HasColumnName("department");
            entity.Property(e => e.EvaluationId).HasColumnName("evaluation_id");
            entity.Property(e => e.Group)
                .HasMaxLength(11)
                .HasColumnName("group");
            entity.Property(e => e.Level)
                .HasMaxLength(3)
                .HasColumnName("level");
            entity.Property(e => e.QuestionSort).HasColumnName("question_sort");
            entity.Property(e => e.QuestionStatement).HasColumnName("question_statement");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(25)
                .HasColumnName("question_type");
            entity.Property(e => e.SubjectCode)
                .HasMaxLength(10)
                .HasColumnName("subject_code");
            entity.Property(e => e.SubjectName)
                .HasMaxLength(75)
                .HasColumnName("subject_name");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.Topic)
                .HasMaxLength(25)
                .HasColumnName("topic");
            entity.Property(e => e.Trainer)
                .HasMaxLength(75)
                .HasColumnName("trainer");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<AnswerDeptInf>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("answer_dept_inf", "reports");

            entity.Property(e => e.Degree)
                .HasMaxLength(4)
                .HasColumnName("degree");
            entity.Property(e => e.Department)
                .HasMaxLength(75)
                .HasColumnName("department");
            entity.Property(e => e.EvaluationId).HasColumnName("evaluation_id");
            entity.Property(e => e.Group)
                .HasMaxLength(11)
                .HasColumnName("group");
            entity.Property(e => e.Level)
                .HasMaxLength(3)
                .HasColumnName("level");
            entity.Property(e => e.QuestionSort).HasColumnName("question_sort");
            entity.Property(e => e.QuestionStatement).HasColumnName("question_statement");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(25)
                .HasColumnName("question_type");
            entity.Property(e => e.SubjectCode)
                .HasMaxLength(10)
                .HasColumnName("subject_code");
            entity.Property(e => e.SubjectName)
                .HasMaxLength(75)
                .HasColumnName("subject_name");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.Topic)
                .HasMaxLength(25)
                .HasColumnName("topic");
            entity.Property(e => e.Trainer)
                .HasMaxLength(75)
                .HasColumnName("trainer");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<AnswerDeptInfMp>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("answer_dept_inf_mp", "reports");

            entity.Property(e => e.Degree)
                .HasMaxLength(4)
                .HasColumnName("degree");
            entity.Property(e => e.Department)
                .HasMaxLength(75)
                .HasColumnName("department");
            entity.Property(e => e.EvaluationId).HasColumnName("evaluation_id");
            entity.Property(e => e.Group)
                .HasMaxLength(11)
                .HasColumnName("group");
            entity.Property(e => e.Level)
                .HasMaxLength(3)
                .HasColumnName("level");
            entity.Property(e => e.QuestionSort).HasColumnName("question_sort");
            entity.Property(e => e.QuestionStatement).HasColumnName("question_statement");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(25)
                .HasColumnName("question_type");
            entity.Property(e => e.SubjectCode)
                .HasMaxLength(10)
                .HasColumnName("subject_code");
            entity.Property(e => e.SubjectName)
                .HasMaxLength(75)
                .HasColumnName("subject_name");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.Topic)
                .HasMaxLength(25)
                .HasColumnName("topic");
            entity.Property(e => e.Trainer)
                .HasMaxLength(75)
                .HasColumnName("trainer");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<AuthGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_group_pkey");

            entity.ToTable("auth_group");

            entity.HasIndex(e => e.Name, "auth_group_name_a6ea08ec_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Name, "auth_group_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
        });

        modelBuilder.Entity<AuthGroupPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_group_permissions_pkey");

            entity.ToTable("auth_group_permissions");

            entity.HasIndex(e => e.GroupId, "auth_group_permissions_group_id_b120cbf9");

            entity.HasIndex(e => new { e.GroupId, e.PermissionId }, "auth_group_permissions_group_id_permission_id_0cd325b0_uniq").IsUnique();

            entity.HasIndex(e => e.PermissionId, "auth_group_permissions_permission_id_84c5c92e");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");

            entity.HasOne(d => d.Group).WithMany(p => p.AuthGroupPermissions)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("auth_group_permissions_group_id_b120cbf9_fk_auth_group_id");

            entity.HasOne(d => d.Permission).WithMany(p => p.AuthGroupPermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("auth_group_permissio_permission_id_84c5c92e_fk_auth_perm");
        });

        modelBuilder.Entity<AuthPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_permission_pkey");

            entity.ToTable("auth_permission");

            entity.HasIndex(e => e.ContentTypeId, "auth_permission_content_type_id_2f476e4b");

            entity.HasIndex(e => new { e.ContentTypeId, e.Codename }, "auth_permission_content_type_id_codename_01ab375a_uniq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Codename)
                .HasMaxLength(100)
                .HasColumnName("codename");
            entity.Property(e => e.ContentTypeId).HasColumnName("content_type_id");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");

            entity.HasOne(d => d.ContentType).WithMany(p => p.AuthPermissions)
                .HasForeignKey(d => d.ContentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("auth_permission_content_type_id_2f476e4b_fk_django_co");
        });

        modelBuilder.Entity<AuthUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_user_pkey");

            entity.ToTable("auth_user");

            entity.HasIndex(e => e.Username, "auth_user_username_6821ab7c_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Username, "auth_user_username_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateJoined).HasColumnName("date_joined");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(150)
                .HasColumnName("first_name");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsStaff).HasColumnName("is_staff");
            entity.Property(e => e.IsSuperuser).HasColumnName("is_superuser");
            entity.Property(e => e.LastLogin).HasColumnName("last_login");
            entity.Property(e => e.LastName)
                .HasMaxLength(150)
                .HasColumnName("last_name");
            entity.Property(e => e.Password)
                .HasMaxLength(128)
                .HasColumnName("password");
            entity.Property(e => e.Username)
                .HasMaxLength(150)
                .HasColumnName("username");
        });

        modelBuilder.Entity<AuthUserGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_user_groups_pkey");

            entity.ToTable("auth_user_groups");

            entity.HasIndex(e => e.GroupId, "auth_user_groups_group_id_97559544");

            entity.HasIndex(e => e.UserId, "auth_user_groups_user_id_6a12ed8b");

            entity.HasIndex(e => new { e.UserId, e.GroupId }, "auth_user_groups_user_id_group_id_94350c0c_uniq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Group).WithMany(p => p.AuthUserGroups)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("auth_user_groups_group_id_97559544_fk_auth_group_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuthUserGroups)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("auth_user_groups_user_id_6a12ed8b_fk_auth_user_id");
        });

        modelBuilder.Entity<AuthUserUserPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_user_user_permissions_pkey");

            entity.ToTable("auth_user_user_permissions");

            entity.HasIndex(e => e.PermissionId, "auth_user_user_permissions_permission_id_1fbb5f2c");

            entity.HasIndex(e => e.UserId, "auth_user_user_permissions_user_id_a95ead1b");

            entity.HasIndex(e => new { e.UserId, e.PermissionId }, "auth_user_user_permissions_user_id_permission_id_14a6b632_uniq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Permission).WithMany(p => p.AuthUserUserPermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("auth_user_user_permi_permission_id_1fbb5f2c_fk_auth_perm");

            entity.HasOne(d => d.User).WithMany(p => p.AuthUserUserPermissions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("auth_user_user_permissions_user_id_a95ead1b_fk_auth_user_id");
        });

        modelBuilder.Entity<Degree>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("degree_pkey");

            entity.ToTable("degree", "master");

            entity.HasIndex(e => e.Code, "uq_degree_unique_code").IsUnique();

            entity.HasIndex(e => e.Name, "uq_degree_unique_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(4)
                .HasColumnName("code");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.LevelId).HasColumnName("level_id");
            entity.Property(e => e.Name)
                .HasMaxLength(75)
                .HasColumnName("name");

            entity.HasOne(d => d.Department).WithMany(p => p.Degrees)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_degree_department_id");

            entity.HasOne(d => d.Level).WithMany(p => p.Degrees)
                .HasForeignKey(d => d.LevelId)
                .HasConstraintName("fk_degree_level_id");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("department_pkey");

            entity.ToTable("department", "master");

            entity.HasIndex(e => e.Code, "uq_department_unique_code").IsUnique();

            entity.HasIndex(e => e.Name, "uq_department_unique_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(3)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(75)
                .HasColumnName("name");
        });

        modelBuilder.Entity<DjangoAdminLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("django_admin_log_pkey");

            entity.ToTable("django_admin_log");

            entity.HasIndex(e => e.ContentTypeId, "django_admin_log_content_type_id_c4bce8eb");

            entity.HasIndex(e => e.UserId, "django_admin_log_user_id_c564eba6");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActionFlag).HasColumnName("action_flag");
            entity.Property(e => e.ActionTime).HasColumnName("action_time");
            entity.Property(e => e.ChangeMessage).HasColumnName("change_message");
            entity.Property(e => e.ContentTypeId).HasColumnName("content_type_id");
            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.ObjectRepr)
                .HasMaxLength(200)
                .HasColumnName("object_repr");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ContentType).WithMany(p => p.DjangoAdminLogs)
                .HasForeignKey(d => d.ContentTypeId)
                .HasConstraintName("django_admin_log_content_type_id_c4bce8eb_fk_django_co");

            entity.HasOne(d => d.User).WithMany(p => p.DjangoAdminLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("django_admin_log_user_id_c564eba6_fk_auth_user_id");
        });

        modelBuilder.Entity<DjangoContentType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("django_content_type_pkey");

            entity.ToTable("django_content_type");

            entity.HasIndex(e => new { e.AppLabel, e.Model }, "django_content_type_app_label_model_76bd3d3b_uniq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppLabel)
                .HasMaxLength(100)
                .HasColumnName("app_label");
            entity.Property(e => e.Model)
                .HasMaxLength(100)
                .HasColumnName("model");
        });

        modelBuilder.Entity<DjangoMigration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("django_migrations_pkey");

            entity.ToTable("django_migrations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.App)
                .HasMaxLength(255)
                .HasColumnName("app");
            entity.Property(e => e.Applied).HasColumnName("applied");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<DjangoSession>(entity =>
        {
            entity.HasKey(e => e.SessionKey).HasName("django_session_pkey");

            entity.ToTable("django_session");

            entity.HasIndex(e => e.ExpireDate, "django_session_expire_date_a5c62663");

            entity.HasIndex(e => e.SessionKey, "django_session_session_key_c0390e0f_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.Property(e => e.SessionKey)
                .HasMaxLength(40)
                .HasColumnName("session_key");
            entity.Property(e => e.ExpireDate).HasColumnName("expire_date");
            entity.Property(e => e.SessionData).HasColumnName("session_data");
        });

        modelBuilder.Entity<DjangoSite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("django_site_pkey");

            entity.ToTable("django_site");

            entity.HasIndex(e => e.Domain, "django_site_domain_a2e37b91_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Domain, "django_site_domain_a2e37b91_uniq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Domain)
                .HasMaxLength(100)
                .HasColumnName("domain");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<FormsAnswer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("forms_answer_pkey");

            entity.ToTable("forms_answer");

            entity.HasIndex(e => e.EvaluationId, "forms_answer_evaluation_id_4380c9c3");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EvaluationId).HasColumnName("evaluation_id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(d => d.Evaluation).WithMany(p => p.FormsAnswers)
                .HasForeignKey(d => d.EvaluationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("forms_answer_evaluation_id_4380c9c3_fk_forms_evaluation_id");

            entity.HasOne(d => d.Question).WithMany(p => p.FormsAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_forms_answer_question_id");
        });

        modelBuilder.Entity<FormsEvaluation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("forms_evaluation_pkey");

            entity.ToTable("forms_evaluation");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.TrainerId).HasColumnName("trainer_id");

            entity.HasOne(d => d.Group).WithMany(p => p.FormsEvaluations)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_forms_evaluation_group_name");

            entity.HasOne(d => d.Subject).WithMany(p => p.FormsEvaluations)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_forms_evaluation_subject_id");

            entity.HasOne(d => d.Trainer).WithMany(p => p.FormsEvaluations)
                .HasForeignKey(d => d.TrainerId)
                .HasConstraintName("fk_forms_evaluation_trainer_id");
        });

        modelBuilder.Entity<FormsParticipation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("forms_participation_pkey");

            entity.ToTable("forms_participation");

            entity.HasIndex(e => e.StudentId, "forms_participation_student_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");

            entity.HasOne(d => d.Student).WithOne(p => p.FormsParticipation)
                .HasForeignKey<FormsParticipation>(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_forms_participation_student_id");
        });

        modelBuilder.Entity<FormsStudent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("forms_student");

            entity.Property(e => e.DegreeCode)
                .HasMaxLength(4)
                .HasColumnName("degree_code");
            entity.Property(e => e.DegreeId).HasColumnName("degree_id");
            entity.Property(e => e.Email)
                .HasMaxLength(75)
                .HasColumnName("email");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.GroupName)
                .HasMaxLength(11)
                .HasColumnName("group_name");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LevelCode)
                .HasMaxLength(3)
                .HasColumnName("level_code");
            entity.Property(e => e.LevelId).HasColumnName("level_id");
            entity.Property(e => e.LevelName)
                .HasMaxLength(25)
                .HasColumnName("level_name");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Subjects).HasColumnName("subjects");
            entity.Property(e => e.Surname)
                .HasMaxLength(50)
                .HasColumnName("surname");
        });

        modelBuilder.Entity<FormsSubject>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("forms_subject");

            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.DegreeCode)
                .HasMaxLength(4)
                .HasColumnName("degree_code");
            entity.Property(e => e.DegreeId).HasColumnName("degree_id");
            entity.Property(e => e.DegreeName)
                .HasMaxLength(75)
                .HasColumnName("degree_name");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.TrainerId).HasColumnName("trainer_id");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("group_pkey");

            entity.ToTable("group", "master");

            entity.HasIndex(e => e.Name, "uq_group_unique_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DegreeId).HasColumnName("degree_id");
            entity.Property(e => e.Name)
                .HasMaxLength(11)
                .HasColumnName("name");

            entity.HasOne(d => d.Degree).WithMany(p => p.Groups)
                .HasForeignKey(d => d.DegreeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_group_degree_id");
        });

        modelBuilder.Entity<Level>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("level_pkey");

            entity.ToTable("level", "master");

            entity.HasIndex(e => e.Code, "uq_level_unique_code").IsUnique();

            entity.HasIndex(e => e.Name, "uq_level_unique_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(3)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(25)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Participation>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("participation", "reports");

            entity.Property(e => e.DegreeName)
                .HasMaxLength(75)
                .HasColumnName("degree_name");
            entity.Property(e => e.DepartmentName)
                .HasMaxLength(75)
                .HasColumnName("department_name");
            entity.Property(e => e.Email)
                .HasMaxLength(75)
                .HasColumnName("email");
            entity.Property(e => e.GroupName)
                .HasMaxLength(11)
                .HasColumnName("group_name");
            entity.Property(e => e.LevelName)
                .HasMaxLength(25)
                .HasColumnName("level_name");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Surname)
                .HasMaxLength(50)
                .HasColumnName("surname");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("question_pkey");

            entity.ToTable("question", "master");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Created).HasColumnName("created");
            entity.Property(e => e.Disabled).HasColumnName("disabled");
            entity.Property(e => e.LevelId).HasColumnName("level_id");
            entity.Property(e => e.Sort).HasColumnName("sort");
            entity.Property(e => e.Statement).HasColumnName("statement");
            entity.Property(e => e.TopicId).HasColumnName("topic_id");
            entity.Property(e => e.TypeId).HasColumnName("type_id");

            entity.HasOne(d => d.Level).WithMany(p => p.Questions)
                .HasForeignKey(d => d.LevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_question_level_id");

            entity.HasOne(d => d.Topic).WithMany(p => p.Questions)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_question_topic_id");

            entity.HasOne(d => d.Type).WithMany(p => p.Questions)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_question_type_id");
        });

        modelBuilder.Entity<SocialaccountSocialaccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("socialaccount_socialaccount_pkey");

            entity.ToTable("socialaccount_socialaccount");

            entity.HasIndex(e => new { e.Provider, e.Uid }, "socialaccount_socialaccount_provider_uid_fc810c6e_uniq").IsUnique();

            entity.HasIndex(e => e.UserId, "socialaccount_socialaccount_user_id_8146e70c");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateJoined).HasColumnName("date_joined");
            entity.Property(e => e.ExtraData).HasColumnName("extra_data");
            entity.Property(e => e.LastLogin).HasColumnName("last_login");
            entity.Property(e => e.Provider)
                .HasMaxLength(30)
                .HasColumnName("provider");
            entity.Property(e => e.Uid)
                .HasMaxLength(191)
                .HasColumnName("uid");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.SocialaccountSocialaccounts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("socialaccount_socialaccount_user_id_8146e70c_fk_auth_user_id");
        });

        modelBuilder.Entity<SocialaccountSocialapp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("socialaccount_socialapp_pkey");

            entity.ToTable("socialaccount_socialapp");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClientId)
                .HasMaxLength(191)
                .HasColumnName("client_id");
            entity.Property(e => e.Key)
                .HasMaxLength(191)
                .HasColumnName("key");
            entity.Property(e => e.Name)
                .HasMaxLength(40)
                .HasColumnName("name");
            entity.Property(e => e.Provider)
                .HasMaxLength(30)
                .HasColumnName("provider");
            entity.Property(e => e.Secret)
                .HasMaxLength(191)
                .HasColumnName("secret");
        });

        modelBuilder.Entity<SocialaccountSocialappSite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("socialaccount_socialapp_sites_pkey");

            entity.ToTable("socialaccount_socialapp_sites");

            entity.HasIndex(e => new { e.SocialappId, e.SiteId }, "socialaccount_socialapp__socialapp_id_site_id_71a9a768_uniq").IsUnique();

            entity.HasIndex(e => e.SiteId, "socialaccount_socialapp_sites_site_id_2579dee5");

            entity.HasIndex(e => e.SocialappId, "socialaccount_socialapp_sites_socialapp_id_97fb6e7d");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SiteId).HasColumnName("site_id");
            entity.Property(e => e.SocialappId).HasColumnName("socialapp_id");

            entity.HasOne(d => d.Site).WithMany(p => p.SocialaccountSocialappSites)
                .HasForeignKey(d => d.SiteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("socialaccount_social_site_id_2579dee5_fk_django_si");

            entity.HasOne(d => d.Socialapp).WithMany(p => p.SocialaccountSocialappSites)
                .HasForeignKey(d => d.SocialappId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("socialaccount_social_socialapp_id_97fb6e7d_fk_socialacc");
        });

        modelBuilder.Entity<SocialaccountSocialtoken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("socialaccount_socialtoken_pkey");

            entity.ToTable("socialaccount_socialtoken");

            entity.HasIndex(e => e.AccountId, "socialaccount_socialtoken_account_id_951f210e");

            entity.HasIndex(e => e.AppId, "socialaccount_socialtoken_app_id_636a42d7");

            entity.HasIndex(e => new { e.AppId, e.AccountId }, "socialaccount_socialtoken_app_id_account_id_fca4e0ac_uniq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.AppId).HasColumnName("app_id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.TokenSecret).HasColumnName("token_secret");

            entity.HasOne(d => d.Account).WithMany(p => p.SocialaccountSocialtokens)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("socialaccount_social_account_id_951f210e_fk_socialacc");

            entity.HasOne(d => d.App).WithMany(p => p.SocialaccountSocialtokens)
                .HasForeignKey(d => d.AppId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("socialaccount_social_app_id_636a42d7_fk_socialacc");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("staff_pkey");

            entity.ToTable("staff", "reports");

            entity.HasIndex(e => e.Email, "uq_staff_unique_email").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(75)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Position)
                .HasMaxLength(50)
                .HasColumnName("position");
            entity.Property(e => e.Surname)
                .HasMaxLength(50)
                .HasColumnName("surname");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("student_pkey");

            entity.ToTable("student", "master");

            entity.HasIndex(e => e.Email, "uq_student_unique_email").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(75)
                .HasColumnName("email");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Surname)
                .HasMaxLength(50)
                .HasColumnName("surname");

            entity.HasOne(d => d.Group).WithMany(p => p.Students)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_student_group_id");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subject_pkey");

            entity.ToTable("subject", "master");

            entity.HasIndex(e => new { e.Code, e.DegreeId }, "uq_subject_unique_code_degree_id").IsUnique();

            entity.HasIndex(e => new { e.Name, e.DegreeId }, "uq_subject_unique_name_degree_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.DegreeId).HasColumnName("degree_id");
            entity.Property(e => e.Name)
                .HasMaxLength(75)
                .HasColumnName("name");
            entity.Property(e => e.TopicId).HasColumnName("topic_id");

            entity.HasOne(d => d.Degree).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.DegreeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_subject_degree_id");

            entity.HasOne(d => d.Topic).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("fk_subject_topic_id");

            entity.HasMany(d => d.Students).WithMany(p => p.Subjects)
                .UsingEntity<Dictionary<string, object>>(
                    "SubjectStudent",
                    r => r.HasOne<Student>().WithMany()
                        .HasForeignKey("StudentId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_subject_student_student_id"),
                    l => l.HasOne<Subject>().WithMany()
                        .HasForeignKey("SubjectId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_subject_student_subject_id"),
                    j =>
                    {
                        j.HasKey("SubjectId", "StudentId").HasName("subject_student_pkey");
                        j.ToTable("subject_student", "master");
                        j.IndexerProperty<int>("SubjectId").HasColumnName("subject_id");
                        j.IndexerProperty<int>("StudentId").HasColumnName("student_id");
                    });
        });

        modelBuilder.Entity<SubjectTrainerGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subject_trainer_group_pkey");

            entity.ToTable("subject_trainer_group", "master");

            entity.HasIndex(e => new { e.SubjectId, e.TrainerId, e.GroupId }, "uq_subject_trainer_group").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.TrainerId).HasColumnName("trainer_id");

            entity.HasOne(d => d.Group).WithMany(p => p.SubjectTrainerGroups)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("fk_subject_trainer_group_group_id");

            entity.HasOne(d => d.Subject).WithMany(p => p.SubjectTrainerGroups)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_subject_trainer_group_subject_id");

            entity.HasOne(d => d.Trainer).WithMany(p => p.SubjectTrainerGroups)
                .HasForeignKey(d => d.TrainerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_subject_trainer_group_trainer_id");
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("topic_pkey");

            entity.ToTable("topic", "master");

            entity.HasIndex(e => e.Name, "uq_topic_unique_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(25)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Trainer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trainer_pkey");

            entity.ToTable("trainer", "master");

            entity.HasIndex(e => e.Name, "uq_trainer_unique_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(75)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Type>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("question_type_pkey");

            entity.ToTable("type", "master");

            entity.HasIndex(e => e.Name, "uq_type_unique_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(25)
                .HasColumnName("name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
