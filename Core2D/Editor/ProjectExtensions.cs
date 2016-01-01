﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Core2D
{
    /// <summary>
    /// The <see cref="Project"/> extension methods.
    /// </summary>
    public static class ProjectExtensions
    {
        /// <summary>
        /// Add document.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="document">The document instance.</param>
        public static void AddDocument(this Project project, Document document)
        {
            if (project.Documents != null && document != null)
            {
                var previous = project.Documents;
                var next = project.Documents.Add(document);
                project.History.Snapshot(previous, next, (p) => project.Documents = p);
                project.Documents = next;
            }
        }

        /// <summary>
        /// Add document at specified index.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="document">The document instance.</param>
        /// <param name="index">The document index.</param>
        public static void AddDocumentAt(this Project project, Document document, int index)
        {
            if (project.Documents != null && document != null && index >= 0)
            {
                var previous = project.Documents;
                var next = project.Documents.Insert(index, document);
                project.History.Snapshot(previous, next, (p) => project.Documents = p);
                project.Documents = next;
            }
        }

        /// <summary>
        /// Remove document object from project <see cref="Project.Documents"/> collection.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="document">The document object to remove from project <see cref="Project.Documents"/> collection.</param>
        public static void RemoveDocument(this Project project, Document document)
        {
            if (project.Documents != null && document != null)
            {
                var previous = project.Documents;
                var next = project.Documents.Remove(document);
                project.History.Snapshot(previous, next, (p) => project.Documents = p);
                project.Documents = next;

                project.CurrentDocument = project.Documents.FirstOrDefault();
                if (project.CurrentDocument != null)
                {
                    project.CurrentContainer = project.CurrentDocument.Pages.FirstOrDefault();
                }
                else
                {
                    project.CurrentContainer = default(Container);
                }
                project.Selected = project.CurrentContainer;
            }
        }

        /// <summary>
        /// Add page.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="document">The document instance.</param>
        /// <param name="page">The page instance.</param>
        public static void AddPage(this Project project, Document document, Page page)
        {
            if (document != null && page != null)
            {
                var previous = document.Pages;
                var next = document.Pages.Add(page);
                project.History.Snapshot(previous, next, (p) => document.Pages = p);
                document.Pages = next;
            }
        }

        /// <summary>
        /// Add page at specified index.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="document">The document instance.</param>
        /// <param name="page">The page instance.</param>
        /// <param name="index">The page index.</param>
        public static void AddPageAt(this Project project, Document document, Page page, int index)
        {
            if (document != null && page != null && index >= 0)
            {
                var previous = document.Pages;
                var next = document.Pages.Insert(index, page);
                project.History.Snapshot(previous, next, (p) => document.Pages = p);
                document.Pages = next;
            }
        }

        /// <summary>
        /// Remove page object from owner document <see cref="Document.Pages"/> collection.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="page">The page object to remove from document <see cref="Document.Pages"/> collection.</param>
        public static void RemovePage(this Project project, Page page)
        {
            if (project.Documents != null && page != null)
            {
                var document = project.Documents.FirstOrDefault(d => d.Pages.Contains(page));
                if (document != null)
                {
                    var previous = document.Pages;
                    var next = document.Pages.Remove(page);
                    project.History.Snapshot(previous, next, (p) => document.Pages = p);
                    document.Pages = next;

                    project.CurrentDocument = document;
                    project.CurrentContainer = document.Pages.FirstOrDefault();
                    project.Selected = project.CurrentContainer;
                }
            }
        }

        /// <summary>
        /// Add template.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="template">The template instance.</param>
        public static void AddTemplate(this Project project, Template template)
        {
            var previous = project.Templates;
            var next = project.Templates.Add(template);
            project.History.Snapshot(previous, next, (p) => project.Templates = p);
            project.Templates = next;
        }

        /// <summary>
        /// Remove template.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="template">The template instance</param>
        public static void RemoveTemplate(this Project project, Template template)
        {
            if (project.Templates != null && template != null)
            {
                var previous = project.Templates;
                var next = project.Templates.Remove(project.CurrentTemplate);
                project.History.Snapshot(previous, next, (p) => project.Templates = p);
                project.Templates = next;

                project.CurrentTemplate = project.Templates.FirstOrDefault();
            }
        }

        /// <summary>
        /// Set page template.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="page">The page instance.</param>
        /// <param name="template">The template instance.</param>
        public static void ApplyTemplate(this Project project, Page page, Template template)
        {
            if (page != null && template != null)
            {
                var previous = page.Template;
                var next = template;
                project.History.Snapshot(previous, next, (p) => page.Template = p);
                page.Template = next;
            }
        }

        /// <summary>
        /// Add layer.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="container">The container instance.</param>
        /// <param name="layer">The layer instance.</param>
        public static void AddLayer(this Project project, Container container, Layer layer)
        {
            if (container != null && layer != null)
            {
                var previous = container.Layers;
                var next = container.Layers.Add(layer);
                project.History.Snapshot(previous, next, (p) => container.Layers = p);
                container.Layers = next;
            }
        }

        /// <summary>
        /// Remove layer.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="layer">The layer instance.</param>
        public static void RemoveLayer(this Project project, Layer layer)
        {
            if (layer != null)
            {
                var container = layer.Owner;
                if (container != null && container.Layers != null)
                {
                    var previous = container.Layers;
                    var next = container.Layers.Remove(layer);
                    project.History.Snapshot(previous, next, (p) => container.Layers = p);
                    container.Layers = next;

                    container.CurrentLayer = container.Layers.FirstOrDefault();
                }
            }
        }

        /// <summary>
        /// Add shape.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="layer">The layer instance.</param>
        /// <param name="shape">The shape instance.</param>
        public static void AddShape(this Project project, Layer layer, BaseShape shape)
        {
            if (layer != null && shape != null)
            {
                var previous = layer.Shapes;
                var next = layer.Shapes.Add(shape);
                project.History.Snapshot(previous, next, (p) => layer.Shapes = p);
                layer.Shapes = next;
            }
        }

        /// <summary>
        /// Remove shape.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="layer">The layer instance.</param>
        /// <param name="shape">The shape instance.</param>
        public static void RemoveShape(this Project project, Layer layer, BaseShape shape)
        {
            if (layer != null && shape != null)
            {
                var previous = layer.Shapes;
                var next = layer.Shapes.Remove(shape);
                project.History.Snapshot(previous, next, (p) => layer.Shapes = p);
                layer.Shapes = next;
            }
        }

        /// <summary>
        /// Remove shape.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="shape">The shape instance.</param>
        public static void RemoveShape(this Project project, BaseShape shape)
        {
            if (shape != null && project.Documents != null)
            {
                var layer = project.Documents.SelectMany(d => d.Pages).SelectMany(p => p.Layers).FirstOrDefault(l => l.Shapes.Contains(shape));
                if (layer != null)
                {
                    project.RemoveShape(layer, shape);
                }
            }
        }

        /// <summary>
        /// Add property.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="data">The data instance.</param>
        /// <param name="property">The property instance.</param>
        public static void AddProperty(this Project project, Data data, Property property)
        {
            if (data != null && data.Properties != null && property != null)
            {
                var previous = data.Properties;
                var next = data.Properties.Add(property);
                project.History.Snapshot(previous, next, (p) => data.Properties = p);
                data.Properties = next;
            }
        }

        /// <summary>
        /// Remove property.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="property">The property instance.</param>
        public static void RemoveProperty(this Project project, Property property)
        {
            if (property != null)
            {
                var owner = property.Owner;
                if (owner is Data)
                {
                    var data = owner;
                    if (data.Properties != null)
                    {
                        var previous = data.Properties;
                        var next = data.Properties.Remove(property);
                        project.History.Snapshot(previous, next, (p) => data.Properties = p);
                        data.Properties = next;
                    }
                }
            }
        }

        /// <summary>
        /// Add database.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="db">The database instance.</param>
        public static void AddDatabase(this Project project, Database db)
        {
            if (project.Databases != null && db != null)
            {
                var previous = project.Databases;
                var next = project.Databases.Add(db);
                project.History.Snapshot(previous, next, (p) => project.Databases = p);
                project.Databases = next;

                project.CurrentDatabase = db;
            }
        }

        /// <summary>
        /// Remove database.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="db">The <see cref="Database"/> to remove.</param>
        public static void RemoveDatabase(this Project project, Database db)
        {
            if (project.Databases != null && db != null)
            {
                var previous = project.Databases;
                var next = project.Databases.Remove(db);
                project.History.Snapshot(previous, next, (p) => project.Databases = p);
                project.Databases = next;

                project.CurrentDatabase = project.Databases.FirstOrDefault();
            }
        }

        /// <summary>
        /// Update the destination database using data from source database.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="destination">The destination database.</param>
        /// <param name="source">The source database.</param>
        public static void UpdateDatabase(this Project project, Database destination, Database source)
        {
            if (source.Columns.Length <= 1)
                return;

            // Check for the Id column.
            if (source.Columns[0].Name != Database.IdColumnName)
                return;

            // Check columns length.
            if (source.Columns.Length - 1 != destination.Columns.Length)
                return;

            // Check column names.
            for (int i = 1; i < source.Columns.Length; i++)
            {
                if (source.Columns[i].Name != destination.Columns[i - 1].Name)
                {
                    return;
                }
            }

            bool isDirty = false;
            var recordsBuilder = destination.Records.ToBuilder();

            // Update or remove existing records.
            for (int i = 0; i < destination.Records.Length; i++)
            {
                var record = destination.Records[i];
                var result = source.Records.FirstOrDefault(r => r.Id == record.Id);
                if (result != null)
                {
                    // Update existing record.
                    for (int j = 1; j < result.Values.Length; j++)
                    {
                        var valuesBuilder = record.Values.ToBuilder();
                        valuesBuilder[j - 1] = result.Values[j];
                        record.Values = valuesBuilder.ToImmutable();
                    }
                    isDirty = true;
                }
                else
                {
                    // Remove existing record.
                    recordsBuilder.Remove(record);
                    isDirty = true;
                }
            }

            // Add new records.
            for (int i = 0; i < source.Records.Length; i++)
            {
                var record = source.Records[i];
                var result = destination.Records.FirstOrDefault(r => r.Id == record.Id);
                if (result == null)
                {
                    var r = source.Records[i];

                    // Use existing columns.
                    r.Columns = destination.Columns;

                    // Skip Id column.
                    r.Values = r.Values.Skip(1).ToImmutableArray();

                    recordsBuilder.Add(r);
                    isDirty = true;
                }
            }

            if (isDirty)
            {
                var builder = project.Databases.ToBuilder();
                var index = builder.IndexOf(destination);
                destination.Records = recordsBuilder.ToImmutable();
                builder[index] = destination;

                var previous = project.Databases;
                var next = builder.ToImmutable();
                project.History.Snapshot(previous, next, (p) => project.Databases = p);
                project.Databases = next;
            }
        }

        /// <summary>
        /// Add column to database columns collection.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="db">The database instance.</param>
        /// <param name="column">The column instance.</param>
        public static void AddColumn(this Project project, Database db, Column column)
        {
            if (db != null && db.Columns != null && column != null)
            {
                var previous = db.Columns;
                var next = db.Columns.Add(column);
                project.History.Snapshot(previous, next, (p) => db.Columns = p);
                db.Columns = next;
            }
        }

        /// <summary>
        /// Remove column from database columns collection.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="column">The <see cref="Column"/> to remove.</param>
        public static void RemoveColumn(this Project project, Column column)
        {
            if (column != null)
            {
                var db = column.Owner;
                if (db != null && db.Columns != null)
                {
                    var previous = db.Columns;
                    var next = db.Columns.Remove(column);
                    project.History.Snapshot(previous, next, (p) => db.Columns = p);
                    db.Columns = next;
                }
            }
        }

        /// <summary>
        /// Add record to database records collection.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="db">The database instance.</param>
        /// <param name="record">The record instance.</param>
        public static void AddRecord(this Project project, Database db, Record record)
        {
            if (db != null && db.Records != null)
            {
                var previous = db.Records;
                var next = db.Records.Add(record);
                project.History.Snapshot(previous, next, (p) => db.Records = p);
                db.Records = next;
            }
        }

        /// <summary>
        /// Remove record from database records collection.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="record">The record instance.</param>
        public static void RemoveRecord(this Project project, Record record)
        {
            if (record != null)
            {
                var db = record.Owner;
                if (db != null && db.Records != null)
                {
                    var previous = db.Records;
                    var next = db.Records.Remove(record);
                    project.History.Snapshot(previous, next, (p) => db.Records = p);
                    db.Records = next;
                }
            }
        }

        /// <summary>
        /// Reset data record.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="data">The data instance.</param>
        public static void ResetRecord(this Project project, Data data)
        {
            if (data != null)
            {
                var record = data.Record;
                if (record != null)
                {
                    var previous = record;
                    var next = default(Record);
                    project.History.Snapshot(previous, next, (p) => data.Record = p);
                    data.Record = next;
                }
            }
        }

        /// <summary>
        /// Set data record.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="data">The data instance.</param>
        /// <param name="record">The record instance.</param>
        public static void ApplyRecord(this Project project, Data data, Record record)
        {
            var previous = data.Record;
            var next = record;
            project.History.Snapshot(previous, next, (p) => data.Record = p);
            data.Record = next;
        }

        /// <summary>
        /// Add group library.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="library">The group library instance.</param>
        public static void AddGroupLibrary(this Project project, Library<XGroup> library)
        {
            if (project.GroupLibraries != null && library != null)
            {
                var previous = project.GroupLibraries;
                var next = project.GroupLibraries.Add(library);
                project.History.Snapshot(previous, next, (p) => project.GroupLibraries = p);
                project.GroupLibraries = next;
            }
        }

        /// <summary>
        /// Remove group library.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="library">The group library instance.</param>
        public static void RemoveGroupLibrary(this Project project, Library<XGroup> library)
        {
            if (project.GroupLibraries != null && library != null)
            {
                var previous = project.GroupLibraries;
                var next = project.GroupLibraries.Remove(library);
                project.History.Snapshot(previous, next, (p) => project.GroupLibraries = p);
                project.GroupLibraries = next;

                project.CurrentGroupLibrary = project.GroupLibraries.FirstOrDefault();
            }
        }

        /// <summary>
        /// Add style library.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="library">The style library instance.</param>
        public static void AddStyleLibrary(this Project project, Library<ShapeStyle> library)
        {
            if (project.StyleLibraries != null)
            {
                var previous = project.StyleLibraries;
                var next = project.StyleLibraries.Add(library);
                project.History.Snapshot(previous, next, (p) => project.StyleLibraries = p);
                project.StyleLibraries = next;
            }
        }

        /// <summary>
        /// Remove style library.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="library">The style library instance.</param>
        public static void RemoveStyleLibrary(this Project project, Library<ShapeStyle> library)
        {
            if (project.CurrentStyleLibrary != null && library != null)
            {
                var previous = project.StyleLibraries;
                var next = project.StyleLibraries.Remove(library);
                project.History.Snapshot(previous, next, (p) => project.StyleLibraries = p);
                project.StyleLibraries = next;

                project.CurrentStyleLibrary = project.StyleLibraries.FirstOrDefault();
            }
        }

        /// <summary>
        /// Add style.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="library">The style library instance.</param>
        /// <param name="style">The style instance.</param>
        public static void AddStyle(this Project project, Library<ShapeStyle> library, ShapeStyle style)
        {
            if (library != null && style != null)
            {
                var previous = library.Items;
                var next = library.Items.Add(style);
                project.History.Snapshot(previous, next, (p) => library.Items = p);
                library.Items = next;
            }
        }

        /// <summary>
        /// Remove style.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="style">The style instance.</param>
        public static void RemoveStyle(this Project project, ShapeStyle style)
        {
            if (project.StyleLibraries != null && style != null)
            {
                var library = project.StyleLibraries.FirstOrDefault(l => l.Items.Contains(style));
                if (library != null)
                {
                    var previous = library.Items;
                    var next = library.Items.Remove(style);
                    project.History.Snapshot(previous, next, (p) => library.Items = p);
                    library.Items = next;

                    library.Selected = library.Items.FirstOrDefault();
                }
            }
        }

        /// <summary>
        /// Set shape style.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="shape">The shape instance.</param>
        /// <param name="style">The style instance.</param>
        public static void ApplyStyle(this Project project, BaseShape shape, ShapeStyle style)
        {
            if (shape != null)
            {
                if (shape is XGroup)
                {
                    var shapes = Editor.GetAllShapes((shape as XGroup).Shapes);
                    foreach (var child in shapes)
                    {
                        var previous = child.Style;
                        var next = style;
                        project.History.Snapshot(previous, next, (p) => child.Style = p);
                        child.Style = next;
                    }
                }
                else
                {
                    var previous = shape.Style;
                    var next = style;
                    project.History.Snapshot(previous, next, (p) => shape.Style = p);
                    shape.Style = next;
                }
            }
        }

        /// <summary>
        /// Add group.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="library">The group library instance.</param>
        /// <param name="group">The group instance.</param>
        public static void AddGroup(this Project project, Library<XGroup> library, XGroup group)
        {
            if (library != null && group != null)
            {
                var previous = library.Items;
                var next = library.Items.Add(group);
                project.History.Snapshot(previous, next, (p) => library.Items = p);
                library.Items = next;
            }
        }

        /// <summary>
        /// Remove group.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="group">The group instance.</param>
        public static void RemoveGroup(this Project project, XGroup group)
        {
            if (project.GroupLibraries != null && group != null)
            {
                var library = project.GroupLibraries.FirstOrDefault(l => l.Items.Contains(group));
                if (library != null)
                {
                    var previous = library.Items;
                    var next = library.Items.Remove(group);
                    project.History.Snapshot(previous, next, (p) => library.Items = p);
                    library.Items = next;

                    library.Selected = library.Items.FirstOrDefault();
                }
            }
        }

        /// <summary>
        /// Group shapes.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="shapes">The selected shapes.</param>
        /// <param name="name">The group name.</param>
        public static XGroup Group(this Project project, ImmutableHashSet<BaseShape> shapes, string name)
        {
            var container = project.CurrentContainer;
            if (container != null)
            {
                var layer = container.CurrentLayer;
                if (layer != null)
                {
                    // TODO: Group method changes SelectedShapes State properties.
                    var g = XGroup.Group(name, shapes);

                    var builder = layer.Shapes.ToBuilder();
                    foreach (var shape in shapes)
                    {
                        builder.Remove(shape);
                    }
                    builder.Add(g);

                    var previous = layer.Shapes;
                    var next = builder.ToImmutable();
                    project.History.Snapshot(previous, next, (p) => layer.Shapes = p);
                    layer.Shapes = next;

                    return g;
                }
            }
            return null;
        }

        /// <summary>
        /// Ungroup shapes.
        /// </summary>
        /// <param name="shapes">The selected shapes.</param>
        /// <param name="original">The ungroup-ed shapes.</param>
        /// <param name="groupShapes">The flag indicating whether shapes originate from group object.</param>
        private static void Ungroup(IEnumerable<BaseShape> shapes, IList<BaseShape> original, bool groupShapes)
        {
            if (shapes != null)
            {
                foreach (var shape in shapes)
                {
                    if (shape is XGroup)
                    {
                        var g = shape as XGroup;
                        Ungroup(g.Shapes, original, groupShapes: true);
                        Ungroup(g.Connectors, original, groupShapes: true);
                        original.Remove(g);
                    }
                    else if (groupShapes)
                    {
                        if (shape is XPoint)
                        {
                            shape.State.Flags &=
                                ~(ShapeStateFlags.Connector
                                | ShapeStateFlags.None
                                | ShapeStateFlags.Input
                                | ShapeStateFlags.Output);
                            shape.State.Flags |= ShapeStateFlags.Standalone;
                            original.Add(shape);
                        }
                        else
                        {
                            shape.State.Flags |= ShapeStateFlags.Standalone;
                            original.Add(shape);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ungroup shapes.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="shape">The selected shape.</param>
        /// <param name="shapes">The selected shapes.</param>
        public static bool Ungroup(this Project project, BaseShape shape, ImmutableHashSet<BaseShape> shapes)
        {
            var container = project.CurrentContainer;
            if (container != null)
            {
                var layer = container.CurrentLayer;
                if (layer != null)
                {
                    if (shape != null && shape is XGroup && layer != null)
                    {
                        var builder = layer.Shapes.ToBuilder();

                        var g = shape as XGroup;
                        Ungroup(g.Shapes, builder, groupShapes: true);
                        Ungroup(g.Connectors, builder, groupShapes: true);
                        builder.Remove(g);

                        var previous = layer.Shapes;
                        var next = builder.ToImmutable();
                        project.History.Snapshot(previous, next, (p) => layer.Shapes = p);
                        layer.Shapes = next;

                        return true;
                    }

                    if (shapes != null && layer != null)
                    {
                        var builder = layer.Shapes.ToBuilder();

                        Ungroup(shapes, builder, groupShapes: false);

                        var previous = layer.Shapes;
                        var next = builder.ToImmutable();
                        project.History.Snapshot(previous, next, (p) => layer.Shapes = p);
                        layer.Shapes = next;

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Move shape from source index to target index position in an array. 
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="source"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="targetIndex"></param>
        private static void Move(this Project project, BaseShape source, int sourceIndex, int targetIndex)
        {
            var container = project.CurrentContainer;
            if (container != null)
            {
                var layer = container.CurrentLayer;
                if (layer != null)
                {
                    var items = layer.Shapes;
                    if (items != null)
                    {
                        if (sourceIndex < targetIndex)
                        {
                            var builder = items.ToBuilder();
                            builder.Insert(targetIndex + 1, source);
                            builder.RemoveAt(sourceIndex);

                            var previous = layer.Shapes;
                            var next = builder.ToImmutable();
                            project.History.Snapshot(previous, next, (p) => layer.Shapes = p);
                            layer.Shapes = next;
                        }
                        else
                        {
                            int removeIndex = sourceIndex + 1;
                            if (items.Length + 1 > removeIndex)
                            {
                                var builder = items.ToBuilder();
                                builder.Insert(targetIndex, source);
                                builder.RemoveAt(removeIndex);

                                var previous = layer.Shapes;
                                var next = builder.ToImmutable();
                                project.History.Snapshot(previous, next, (p) => layer.Shapes = p);
                                layer.Shapes = next;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Bring a shape to the top of the stack.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="source"></param>
        public static void BringToFront(this Project project, BaseShape source)
        {
            var container = project.CurrentContainer;
            if (container != null)
            {
                var layer = container.CurrentLayer;
                if (layer != null)
                {
                    var items = layer.Shapes;
                    int sourceIndex = items.IndexOf(source);
                    int targetIndex = items.Length - 1;
                    if (targetIndex >= 0 && sourceIndex != targetIndex)
                    {
                        project.Move(source, sourceIndex, targetIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Move a shape one step closer to the front of the stack.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="source"></param>
        public static void BringForward(this Project project, BaseShape source)
        {
            var container = project.CurrentContainer;
            if (container != null)
            {
                var layer = container.CurrentLayer;
                if (layer != null)
                {
                    var items = layer.Shapes;
                    int sourceIndex = items.IndexOf(source);
                    int targetIndex = sourceIndex + 1;
                    if (targetIndex < items.Length)
                    {
                        project.Move(source, sourceIndex, targetIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Move a shape one step down within the stack.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="source"></param>
        public static void SendBackward(this Project project, BaseShape source)
        {
            var container = project.CurrentContainer;
            if (container != null)
            {
                var layer = container.CurrentLayer;
                if (layer != null)
                {
                    var items = layer.Shapes;
                    int sourceIndex = items.IndexOf(source);
                    int targetIndex = sourceIndex - 1;
                    if (targetIndex >= 0)
                    {
                        project.Move(source, sourceIndex, targetIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Move a shape to the bottom of the stack.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="source"></param>
        public static void SendToBack(this Project project, BaseShape source)
        {
            var container = project.CurrentContainer;
            if (container != null)
            {
                var layer = container.CurrentLayer;
                if (layer != null)
                {
                    var items = layer.Shapes;
                    int sourceIndex = items.IndexOf(source);
                    int targetIndex = 0;
                    if (sourceIndex != targetIndex)
                    {
                        project.Move(source, sourceIndex, targetIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Move shape(s) by specified offset.
        /// </summary>
        /// <param name="project">The project instance.</param>
        /// <param name="shape">The selected shape.</param>
        /// <param name="shapes">The selected shapes.</param>
        /// <param name="dx">The X coordinate offset.</param>
        /// <param name="dy">The Y coordinate offset.</param>
        public static void MoveBy(this Project project, BaseShape shape, ImmutableHashSet<BaseShape> shapes, double dx, double dy)
        {
            if (shape != null)
            {
                var state = shape.State;

                switch (project.Options.MoveMode)
                {
                    case MoveMode.Point:
                        {
                            if (!state.Flags.HasFlag(ShapeStateFlags.Locked))
                            {
                                var items = Enumerable.Repeat(shape, 1);
                                var points = items.SelectMany(s => s.GetPoints()).Distinct().ToList();

                                Editor.MovePointsBy(points, dx, dy);

                                var previous = new { DeltaX = -dx, DeltaY = -dy, Points = points };
                                var next = new { DeltaX = dx, DeltaY = dy, Points = points };
                                project.History.Snapshot(previous, next, (s) => Editor.MovePointsBy(s.Points, s.DeltaX, s.DeltaY));
                            }
                        }
                        break;
                    case MoveMode.Shape:
                        {
                            if (!state.Flags.HasFlag(ShapeStateFlags.Locked) && !state.Flags.HasFlag(ShapeStateFlags.Connector))
                            {
                                var items = Enumerable.Repeat(shape, 1).ToList();

                                Editor.MoveShapesBy(items, dx, dy);

                                var previous = new { DeltaX = -dx, DeltaY = -dy, Shapes = items };
                                var next = new { DeltaX = dx, DeltaY = dy, Shapes = items };
                                project.History.Snapshot(previous, next, (s) => Editor.MoveShapesBy(s.Shapes, s.DeltaX, s.DeltaY));
                            }
                        }
                        break;
                }
            }

            if (shapes != null)
            {
                var items = shapes.Where(s => !s.State.Flags.HasFlag(ShapeStateFlags.Locked));

                switch (project.Options.MoveMode)
                {
                    case MoveMode.Point:
                        {
                            var points = items.SelectMany(s => s.GetPoints()).Distinct().ToList();

                            Editor.MovePointsBy(points, dx, dy);

                            var previous = new { DeltaX = -dx, DeltaY = -dy, Points = points };
                            var next = new { DeltaX = dx, DeltaY = dy, Points = points };
                            project.History.Snapshot(previous, next, (s) => Editor.MovePointsBy(s.Points, s.DeltaX, s.DeltaY));
                        }
                        break;
                    case MoveMode.Shape:
                        {
                            Editor.MoveShapesBy(items, dx, dy);

                            var previous = new { DeltaX = -dx, DeltaY = -dy, Shapes = items.ToList() };
                            var next = new { DeltaX = dx, DeltaY = dy, Shapes = items.ToList() };
                            project.History.Snapshot(previous, next, (s) => Editor.MoveShapesBy(s.Shapes, s.DeltaX, s.DeltaY));
                        }
                        break;
                }
            }
        }
    }
}
