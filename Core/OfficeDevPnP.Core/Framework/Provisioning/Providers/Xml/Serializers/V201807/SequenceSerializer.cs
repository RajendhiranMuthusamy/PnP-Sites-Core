﻿using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml.Resolvers;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml.Resolvers.V201801;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml.Resolvers.V201805;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml.Resolvers.V201807;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml.Serializers
{
    /// <summary>
    /// Class to serialize/deserialize the Tenant-wide settings
    /// </summary>
    [TemplateSchemaSerializer(
        MinimalSupportedSchemaVersion = XMLPnPSchemaVersion.V201807,
        SerializationSequence = -1, DeserializationSequence = -1,
        Default = false)]
    internal class SequenceSerializer : PnPBaseSchemaSerializer<ProvisioningSequence>
    {
        public override void Deserialize(object persistence, ProvisioningTemplate template)
        {
            var sequences = persistence.GetPublicInstancePropertyValue("Sequence");

            if (sequences != null)
            {
                var expressions = new Dictionary<Expression<Func<ProvisioningSequence, Object>>, IResolver>();

                // Handle the TermStore property of the Sequence, if any
                expressions.Add(seq => seq.TermStore, new ExpressionValueResolver((s, v) => {

                    if (v != null)
                    {
                        var tgs = new TermGroupsSerializer();
                        var termGroupsExpressions = tgs.GetTermGroupExpressions();

                        var result = new Model.ProvisioningTermStore();
                        result.TermGroups.AddRange(
                            PnPObjectsMapper.MapObjects<TermGroup>(v,
                                new CollectionFromSchemaToModelTypeResolver(typeof(TermGroup)),
                                termGroupsExpressions,
                                recursive: true)
                                as IEnumerable<TermGroup>);

                        return (result);
                    }
                    else
                    {
                        return (null);
                    }
                }));

                // Handle the SiteCollections property of the Sequence, if any
                expressions.Add(seq => seq.SiteCollections, 
                    new SiteCollectionsAndSitesFromSchemaToModelTypeResolver(typeof(SiteCollection)));
                expressions.Add(seq => seq.SiteCollections[0].Sites,
                    new SiteCollectionsAndSitesFromSchemaToModelTypeResolver(typeof(SubSite)));
                expressions.Add(seq => seq.SiteCollections[0].Sites[0].Sites,
                    new SiteCollectionsAndSitesFromSchemaToModelTypeResolver(typeof(SubSite)));
                expressions.Add(seq => seq.SiteCollections[0].Templates, new ExpressionValueResolver((s, v) => {

                    var result = new List<String>();

                    foreach (var t in (IEnumerable)v)
                    {
                        var templateId = t.GetPublicInstancePropertyValue("ID")?.ToString();

                        if (templateId != null)
                        {
                            result.Add(templateId);
                        }
                    }

                    return (result);
                }));
                expressions.Add(seq => seq.SiteCollections[0].Sites[0].Templates, new ExpressionValueResolver((s, v) => {

                    var result = new List<String>();

                    foreach (var t in (IEnumerable)v)
                    {
                        var templateId = t.GetPublicInstancePropertyValue("ID")?.ToString();

                        if (templateId != null)
                        {
                            result.Add(templateId);
                        }
                    }

                    return (result);
                }));

                template.ParentHierarchy.Sequences.AddRange(
                PnPObjectsMapper.MapObjects<ProvisioningSequence>(sequences,
                        new CollectionFromSchemaToModelTypeResolver(typeof(ProvisioningSequence)),
                        expressions, 
                        recursive: true)
                        as IEnumerable<ProvisioningSequence>);
            }
        }

        public override void Serialize(ProvisioningTemplate template, object persistence)
        {
            if (template.ParentHierarchy != null && 
                template.ParentHierarchy.Sequences != null &&
                template.ParentHierarchy.Sequences.Count > 0)
            {
                var sequenceTypeName = $"{PnPSerializationScope.Current?.BaseSchemaNamespace}.Sequence, {PnPSerializationScope.Current?.BaseSchemaAssemblyName}";
                var sequenceType = Type.GetType(sequenceTypeName, true);

                persistence.GetPublicInstanceProperty("Sequence")
                    .SetValue(
                        persistence,
                        PnPObjectsMapper.MapObjects(template.ParentHierarchy.Sequences,
                            new CollectionFromModelToSchemaTypeResolver(sequenceType), recursive: true));
            }
        }
    }
}