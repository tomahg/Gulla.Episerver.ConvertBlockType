﻿using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using EPiServer;
using EPiServer.Core;
using EPiServer.Data;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.ServiceLocation;

namespace Gulla.Episerver.BlockConverter
{
    /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice.</summary>
    /// <internal-api />
    /// <exclude />
    [ServiceConfiguration]
    public class ConvertBlockTypeDb : DataAccessBase
    {
        private readonly IContentRepository _contentRepository;
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly IPropertyDefinitionRepository _propertyDefinitionRepository;

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. Initializes a new instance of the <see cref="T:TinyMCE.Business.ConvertBlocks.ConvertBlockTypeDB" /> class.
        /// </summary>
        /// <exclude />
        public ConvertBlockTypeDb(
          IDatabaseExecutor databaseHandler,
          IContentRepository contentRepository,
          ILanguageBranchRepository languageBranchRepository,
          IPropertyDefinitionRepository propertyDefinitionRepository)
          : base(databaseHandler)
        {
            _contentRepository = contentRepository;
            _languageBranchRepository = languageBranchRepository;
            _propertyDefinitionRepository = propertyDefinitionRepository;
        }

        /// <summary>Unsupported INTERNAL API! Not covered by semantic versioning; might change without notice. Convert a block to a new block type
        /// This member supports the EPiServer infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="blockLinkId">The id to the block which will be converted</param>
        /// <param name="fromBlockTypeId">The id of the block type to convert from</param>
        /// <param name="toBlockTypeId">To id of the block type to convert to</param>
        /// <param name="propertyTypeMap">"from"-"to" mappings of properties
        /// , the mapped properties has to be on the same base form</param>
        /// <param name="recursive">if set to <c>true</c> the conversion will be performed for all child blocks also</param>
        /// <param name="isTest">if set to <c>true</c> no actual conversion is made but a test to see the effect of the conversion</param>
        /// <returns>A dataset with information of changes</returns>
        /// <exclude />
        public virtual DataSet Convert(
          int blockLinkId,
          int fromBlockTypeId,
          int toBlockTypeId,
          List<KeyValuePair<int, int>> propertyTypeMap,
          bool recursive,
          bool isTest)
        {
            return Executor.ExecuteTransaction(() => new DataSet()
            {
                Locale = CultureInfo.InvariantCulture,
                Tables = {
                    ConvertPageTypeProperties(blockLinkId, fromBlockTypeId, propertyTypeMap, recursive, isTest),
                    ConvertPageType(blockLinkId, fromBlockTypeId, toBlockTypeId, recursive, isTest)
                }
            });
        }

        private DataTable ConvertPageTypeProperties(
          int blockLinkId,
          int fromBlockTypeId,
          List<KeyValuePair<int, int>> propertyTypeMap,
          bool recursive,
          bool isTest)
        {
            DataTable dataTable = new DataTable("Properties");
            dataTable.Locale = CultureInfo.InvariantCulture;
            dataTable.Columns.Add("FromPropertyID");
            dataTable.Columns.Add("ToPropertyID");
            dataTable.Columns.Add("Count");

            var content = (ILocalizable)_contentRepository.Get<IContent>(new ContentReference(blockLinkId));
            var languageBranch = content.MasterLanguage;
            int id = _languageBranchRepository.Load(languageBranch).ID;

            foreach (KeyValuePair<int, int> propertyType in propertyTypeMap)
            {
                DbCommand command = CreateCommand("netConvertPropertyForPageType");
                command.Parameters.Add(CreateReturnParameter());
                command.Parameters.Add(CreateParameter("PageID", blockLinkId));
                command.Parameters.Add(CreateParameter("FromPageType", fromBlockTypeId));
                command.Parameters.Add(CreateParameter("FromPropertyID", propertyType.Key));
                command.Parameters.Add(CreateParameter("ToPropertyID", propertyType.Value));
                command.Parameters.Add(CreateParameter("Recursive", recursive));
                command.Parameters.Add(CreateParameter("MasterLanguageID", id));
                command.Parameters.Add(CreateParameter("IsTest", isTest));
                command.ExecuteNonQuery();
                DataRow row = dataTable.NewRow();
                row[0] = propertyType.Key;
                row[1] = propertyType.Value;
                row[2] = GetReturnValue(command);
                dataTable.Rows.Add(row);
                if (_propertyDefinitionRepository.Load(propertyType.Key).Type.DataType == PropertyDataType.Category)
                {
                    command.CommandText = "netConvertCategoryPropertyForPageType";
                    command.ExecuteNonQuery();
                }
            }
            return dataTable;
        }

        private DataTable ConvertPageType(
          int blockLinkId,
          int fromBlockTypeId,
          int toBlockTypeId,
          bool recursive,
          bool isTest)
        {
            DataTable dataTable = new DataTable("Pages");
            dataTable.Locale = CultureInfo.InvariantCulture;
            dataTable.Columns.Add("Count");
            DbCommand command = CreateCommand("netConvertPageType");
            command.Parameters.Add(CreateReturnParameter());
            command.Parameters.Add(CreateParameter("PageID", blockLinkId));
            command.Parameters.Add(CreateParameter("FromPageType", fromBlockTypeId));
            command.Parameters.Add(CreateParameter("ToPageType", toBlockTypeId));
            command.Parameters.Add(CreateParameter("Recursive", recursive));
            command.Parameters.Add(CreateParameter("IsTest", isTest));
            command.ExecuteNonQuery();
            DataRow row = dataTable.NewRow();
            row["Count"] = GetReturnValue(command);
            dataTable.Rows.Add(row);
            return dataTable;
        }
    }
}
