using GymManager.Domain.Branches;
using GymManager.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.MemberCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(m => m.MemberCode).IsUnique();

        builder.Property(m => m.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.LastName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.PhoneNumber).HasMaxLength(30).IsRequired();

        builder.Property(m => m.CheckInCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(m => m.CheckInCode).IsUnique();

        builder.Property(m => m.ProfileImageUrl).HasMaxLength(500);
        builder.Property(m => m.EmergencyContactName).HasMaxLength(150);
        builder.Property(m => m.EmergencyContactPhone).HasMaxLength(30);

        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Gender).HasConversion<string>().HasMaxLength(20);

        builder.Property(m => m.CreatedBy).HasMaxLength(256);
        builder.Property(m => m.ModifiedBy).HasMaxLength(256);
        builder.Property(m => m.DeletedBy).HasMaxLength(256);

        builder.OwnsOne(m => m.Email, email =>
        {
            email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(256);
            email.HasIndex(e => e.Value).IsUnique().HasFilter("[Email] IS NOT NULL");
        });

        builder.OwnsOne(m => m.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("State").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(100);
        });

        builder.OwnsOne(m => m.MedicalInfo, medicalInfo =>
        {
            medicalInfo.Property(mi => mi.BloodType).HasColumnName("MedicalBloodType").HasMaxLength(10);
            medicalInfo.Property(mi => mi.Conditions).HasColumnName("MedicalConditions").HasMaxLength(1000);
            medicalInfo.Property(mi => mi.Allergies).HasColumnName("MedicalAllergies").HasMaxLength(1000);
            medicalInfo.Property(mi => mi.Medications).HasColumnName("MedicalMedications").HasMaxLength(1000);
            medicalInfo.Property(mi => mi.Notes).HasColumnName("MedicalNotes").HasMaxLength(2000);
        });

        builder.OwnsMany(m => m.Documents, document =>
        {
            document.ToTable("MemberDocuments");
            document.WithOwner().HasForeignKey("MemberId");
            document.HasKey(d => d.Id);
            document.Property(d => d.Id).ValueGeneratedNever();

            document.Property(d => d.FileName).HasMaxLength(260).IsRequired();
            document.Property(d => d.FileUrl).HasMaxLength(500).IsRequired();
            document.Property(d => d.DocumentType).HasConversion<string>().HasMaxLength(30);
            document.Property(d => d.UploadedBy).HasMaxLength(256);
        });

        builder.Property(m => m.RowVersion).IsRowVersion();

        builder.HasIndex(m => m.BranchId);

        // Shadow (no-navigation) FK — see LeadConfiguration for the rationale.
        builder.HasOne<Branch>().WithMany().HasForeignKey(m => m.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
