using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using TaskManagement.Tasks;
using TaskManagement.Projects; // Thêm namespace mới

namespace TaskManagement.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class TaskManagementDbContext :
    AbpDbContext<TaskManagementDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    // Identity & Tenant Management (Giữ nguyên)
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    // Đưa các thực thể nghiệp vụ vào DbContext
    public DbSet<AppTask> Tasks { get; set; }
    public DbSet<Project> Projects { get; set; } // Bảng dự án mới
    public DbSet<ProjectMember> ProjectMembers { get; set; } // Bảng thành viên dự án

    public TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Cấu hình các module mặc định của ABP (Giữ nguyên)
        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();

        // --- CẤU HÌNH NGHIỆP VỤ ---

        // 1. Cấu hình Project
        builder.Entity<Project>(b =>
        {
            b.ToTable(TaskManagementConsts.DbTablePrefix + "Projects", TaskManagementConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);

            // Quan hệ 1-n: Một dự án có nhiều thành viên
            b.HasMany(x => x.Members).WithOne().HasForeignKey(x => x.ProjectId).IsRequired();
        });

        // 2. Cấu hình ProjectMember (Khóa chính kết hợp)
        builder.Entity<ProjectMember>(b =>
        {
            b.ToTable(TaskManagementConsts.DbTablePrefix + "ProjectMembers", TaskManagementConsts.DbSchema);
            b.ConfigureByConvention();
            b.HasKey(x => new { x.ProjectId, x.UserId }); // Định nghĩa Composite Key
        });

        // 3. Cấu hình AppTask (Giữ Collation và thêm quan hệ Project)
        builder.Entity<AppTask>(b =>
        {
            b.ToTable(TaskManagementConsts.DbTablePrefix + "Tasks", TaskManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(256);

            b.Property(x => x.Description);

            // Quan hệ N-1: Nhiều Task thuộc về 1 Project
            b.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).IsRequired();
        });

        // 4. Cấu hình TaskAssignment (Khóa chính kết hợp và quan hệ)
        builder.Entity<TaskAssignment>(b =>
        {
            // Định nghĩa bảng trung gian
            b.ToTable(TaskManagementConsts.DbTablePrefix + "TaskAssignments", TaskManagementConsts.DbSchema);

            // Cấu hình chuẩn ABP
            b.ConfigureByConvention();

            // QUAN TRỌNG: Định nghĩa Khóa chính hợp thành từ 2 ID
            b.HasKey(x => new { x.TaskId, x.UserId });

            // Cấu hình quan hệ (Optional nhưng nên có để dữ liệu chặt chẽ)
            b.HasOne<AppTask>().WithMany(x => x.Assignments).HasForeignKey(x => x.TaskId).IsRequired();
        });

        //5 Cấu hình AppTask để làm cho cột DeletionReason không bắt buộc
        builder.Entity<AppTask>(b =>
        {
            b.ToTable(TaskManagementConsts.DbTablePrefix + "Tasks", TaskManagementConsts.DbSchema);
            b.ConfigureByConvention();

            // THÊM: Cấu hình cột này không bắt buộc (Nullable) để fix lỗi INSERT
            b.Property(x => x.DeletionReason).IsRequired(false).HasMaxLength(500);
        });
    }
}