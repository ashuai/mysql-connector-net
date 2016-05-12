﻿// Copyright © 2015, 2016 Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySQL.Data.Entity.Metadata;
using System.Linq;

namespace MySQL.Data.Entity.Migrations
{
  public class MySQLMigrationsSqlGenerator : MigrationsSqlGenerator
  {
    private readonly ISqlGenerationHelper _sqlGenerationHelper;

    public MySQLMigrationsSqlGenerator(
        [NotNull] IRelationalCommandBuilderFactory commandBuilderFactory,
        [NotNull] ISqlGenerationHelper sqlGenerationHelper,
        [NotNull] IRelationalTypeMapper typeMapper,
        [NotNull] IRelationalAnnotationProvider annotations)
            : base(commandBuilderFactory, sqlGenerationHelper, typeMapper, annotations)
    {
      _sqlGenerationHelper = sqlGenerationHelper;
    }

    protected override void Generate(
      [NotNull] MigrationOperation operation, 
      [CanBeNull] IModel model, 
      [NotNull] RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "operation");
      ThrowIf.Argument.IsNull(builder, "builder");

      if (operation is MySQLCreateDatabaseOperation)
        Generate(operation as MySQLCreateDatabaseOperation, model, builder);
      else if (operation is MySQLDropDatabaseOperation)
        Generate(operation as MySQLDropDatabaseOperation, model, builder);
      else
        base.Generate(operation, model, builder);
    }

    protected override void Generate(
      [NotNull] EnsureSchemaOperation operation, 
      [CanBeNull] IModel model, 
      [NotNull] RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "operation");
      ThrowIf.Argument.IsNull(builder, "builder");

      throw new NotImplementedException();
      //base.Generate(operation, model, builder);
    }

    protected virtual void Generate(
        [NotNull] MySQLCreateDatabaseOperation operation,
        [CanBeNull] IModel model,
        [NotNull] RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "operation");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
          .Append("CREATE DATABASE ")
          .Append(_sqlGenerationHelper.DelimitIdentifier(operation.Name));
    }

    protected virtual void Generate(
        [NotNull] MySQLDropDatabaseOperation operation,
        [CanBeNull] IModel model,
        [NotNull] RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "operation");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
          .Append("DROP DATABASE IF EXISTS ")
          .Append(_sqlGenerationHelper.DelimitIdentifier(operation.Name));
    }
    

    protected override void ColumnDefinition(
      [CanBeNull] string schema, 
      [CanBeNull] string table, 
      [NotNull] string name, 
      [NotNull] Type clrType, 
      [CanBeNull] string type, 
      bool nullable, 
      [CanBeNull] object defaultValue, 
      [CanBeNull] string defaultValueSql, 
      [CanBeNull] string computedColumnSql, 
      [NotNull] IAnnotatable annotatable, 
      [CanBeNull] IModel model, 
      [NotNull] RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsEmpty(name, "name");
      ThrowIf.Argument.IsNull(clrType, "clrType");
      ThrowIf.Argument.IsNull(annotatable, "annotatable");
      ThrowIf.Argument.IsNull(builder, "builder");

      var property = FindProperty(model, schema, table, name);

      if (type == null)
      {        
        type = property != null
            ? TypeMapper.GetMapping(property).DefaultTypeName
            : TypeMapper.GetMapping(clrType).DefaultTypeName;
      }      

      if (computedColumnSql != null)
      {
         builder
              .Append(_sqlGenerationHelper.DelimitIdentifier(name))
              .Append(string.Format(" {0} AS ", type))
              .Append(" (" + computedColumnSql + ")");

          return;
              
      }

      if (property != null && defaultValue != null && property.ClrType == typeof(string))
      {
        defaultValue = "'" + defaultValue + "'";
      }
            
      var autoInc = annotatable[MySQLAnnotationNames.Prefix + MySQLAnnotationNames.AutoIncrement];

      base.ColumnDefinition(
      schema,
      table,
      name,
      clrType,
      type,
      nullable,
      defaultValue,
      defaultValueSql,
      computedColumnSql,
      annotatable,
      model,
      builder);

      if (autoInc != null && (bool)autoInc)
      {
        builder.Append(" AUTO_INCREMENT");
      }      
    }


    protected override void DefaultValue(
           [CanBeNull] object defaultValue,
           [CanBeNull] string defaultValueSql,
           [NotNull] RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(builder, nameof(builder));

      if (defaultValueSql != null)
      {
        builder
            .Append(" DEFAULT ")
            .Append(defaultValueSql);            
      }
      else if (defaultValue != null)
      {
        builder
            .Append(" DEFAULT ")
            .Append(defaultValue);
      }
    }



    protected override void PrimaryKeyConstraint(
         [NotNull] AddPrimaryKeyOperation operation,
         [CanBeNull] IModel model,
         [NotNull] RelationalCommandListBuilder builder)
    {

      ThrowIf.Argument.IsNull(operation, "AddPrimaryKeyOperation");
      ThrowIf.Argument.IsNull(builder, "RelationalCommandListBuider");


      //MySQL always assign PRIMARY to the PK name no way to override that.
      // check http://dev.mysql.com/doc/refman/5.1/en/create-table.html
      
      builder
          .Append("PRIMARY KEY ")
          .Append("(")
          .Append(string.Join(", ", operation.Columns.Select(_sqlGenerationHelper.DelimitIdentifier)))
          .Append(")");

      IndexTraits(operation, model, builder);
    }

    protected override void Generate(AlterColumnOperation operation, IModel model, RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "AlterColumnOperation");
      ThrowIf.Argument.IsNull(model, "model");
      ThrowIf.Argument.IsNull(builder, "builder");

      var operationColumn = new AddColumnOperation();
      operationColumn.Schema = operation.Schema;
      operationColumn.Table = operation.Table;
      operationColumn.Name = operation.Name;
      operationColumn.ClrType = operation.ClrType;
      operationColumn.ColumnType = operation.ColumnType;
      operationColumn.ComputedColumnSql = operation.ComputedColumnSql;
      operationColumn.DefaultValue = operation.DefaultValue;
      operationColumn.DefaultValueSql = operation.DefaultValueSql;

      builder
       .Append("ALTER TABLE " + operation.Table)
       .Append(" MODIFY ");

      ColumnDefinition(operationColumn, model, builder);      
    }

    protected override void Generate(RenameTableOperation operation, IModel model, RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "RenameTableOperation");
      ThrowIf.Argument.IsNull(model, "model");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
      .Append("ALTER TABLE " + operation.Name)
      .Append(" RENAME " + operation.NewName);
    }

    protected override void Generate(CreateIndexOperation operation, IModel model, RelationalCommandListBuilder builder)
    {

      ThrowIf.Argument.IsNull(operation, "CreateIndexOperation");
      ThrowIf.Argument.IsNull(model, "model");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
      .Append("CREATE " + (operation.IsUnique ? "UNIQUE " : "") + "INDEX ");
      
      builder.Append(_sqlGenerationHelper.DelimitIdentifier(operation.Name) + " ON " + operation.Table + " (" + string.Join(", ", operation.Columns.Select(_sqlGenerationHelper.DelimitIdentifier)) + ")");     
    }

    protected override void Generate(RenameIndexOperation operation, IModel model, RelationalCommandListBuilder builder)
    {

      throw new NotSupportedException();

      ThrowIf.Argument.IsNull(operation, "RenameIndexOperation");
      ThrowIf.Argument.IsNull(model, "model");
      ThrowIf.Argument.IsNull(builder, "builder");      

      //table content remains the same
      builder
      .Append("DROP INDEX ")
      .Append(_sqlGenerationHelper.DelimitIdentifier(operation.Name) + ", ")
      .Append("CREATE INDEX " )
      .Append(_sqlGenerationHelper.DelimitIdentifier(operation.Name) + " ON " + operation.Table );
    }

    protected override void Generate(DropIndexOperation operation, IModel model, RelationalCommandListBuilder builder)
    {
      ThrowIf.Argument.IsNull(operation, "DropIndexOperation");
      ThrowIf.Argument.IsNull(model, "model");
      ThrowIf.Argument.IsNull(builder, "builder");

      builder
      .Append("DROP INDEX ")
      .Append(operation.Name)
      .Append(" ON " + operation.Table);            
    }
  }
}
