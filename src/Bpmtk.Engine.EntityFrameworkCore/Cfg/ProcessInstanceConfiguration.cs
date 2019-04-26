﻿using System;
using System.Collections.Generic;
using System.Text;
using Bpmtk.Engine.Models;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Bpmtk.Engine.Cfg
{
    public class ProcessInstanceConfiguration : IEntityTypeConfiguration<ProcessInstance>
    {
        public virtual void Configure(EntityTypeBuilder<ProcessInstance> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.ProcessDefinition)
                .WithMany()
                .HasForeignKey("ProcessDefinitionId")
                .OnDelete(DeleteBehavior.Restrict);

            //identity-links.
            builder.HasMany(x => x.IdentityLinks)
                .WithOne()
                .HasForeignKey("ProcessInstanceId")
                .OnDelete(DeleteBehavior.Cascade);

            //Variables
            builder.HasMany(x => x.Variables)
               .WithOne()
               .HasForeignKey("ProcessInstanceId")
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Initiator)
                .WithMany()
                .HasForeignKey("InitiatorId")
                .OnDelete(DeleteBehavior.Restrict);

            //builder.Property(x => x.De).HasColumnName("department_id");
            builder.Property(x => x.TenantId);

            builder.Property(x => x.Key).HasMaxLength(32);
            builder.Property(x => x.Name).IsRequired(true).HasMaxLength(100);
            builder.Property(x => x.LastStateTime).IsRequired(true);
            builder.Property(x => x.StartTime);
            builder.Property(x => x.EndReason).HasMaxLength(255);

            builder.HasMany(x => x.Tokens)
                .WithOne(x => x.ProcessInstance)
                .HasForeignKey("ProcessInstanceId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Caller)
                .WithMany()
                .HasForeignKey("CallerId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Super)
                .WithMany()
                .HasForeignKey(x => x.SuperId) //这里不这么弄有bug
                //.HasForeignKey("SuperId")
                .OnDelete(DeleteBehavior.Restrict);

            //mark concurrency token.
            builder.Property(x => x.ConcurrencyStamp)
                .IsConcurrencyToken();

            builder.Property(x => x.Description)
                .HasMaxLength(255);

            builder.ApplyNamingStrategy();
        }
    }
}