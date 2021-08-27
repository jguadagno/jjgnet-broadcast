using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.AzureHelpers.Cosmos;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Data.Repositories
{
    public class EngagementRepository
    {
        private readonly Table _engagementTable;
        private readonly Table _talkTable;
        
        public EngagementRepository(string connectionString)
        {
            _engagementTable = new Table(connectionString, Constants.Tables.Engagements);
            _talkTable = new Table(connectionString, Constants.Tables.Talks);
        
        }

        public async Task<Engagement> GetAsync(string partitionKey, string rowKey)
        {
            var engagement = await _engagementTable.GetTableEntityAsync<Engagement>(partitionKey, rowKey);
            var talks = await _talkTable.GetPartitionAsync<Talk>(rowKey);
            engagement.Talks = talks;

            return engagement;
        }
        
        public async Task<bool> SaveAsync(Engagement engagement)
        {
            
            var tableResult = await _engagementTable.InsertOrReplaceEntityAsync(engagement);
            if (!tableResult.WasSuccessful)
            {
                return false;
            }

            foreach (var talk in engagement.Talks)
            {
                if (string.IsNullOrEmpty(talk.PartitionKey))
                {
                    talk.PartitionKey = engagement.RowKey;
                }

                if (string.IsNullOrEmpty(talk.RowKey))
                {
                    talk.RowKey = Guid.NewGuid().ToString();
                }

                tableResult = await _talkTable.InsertOrReplaceEntityAsync(talk);
                if (tableResult.WasSuccessful == false)
                {
                    return false;
                }
            }

            return true;
        }
       
        public async Task<bool> AddAllAsync(List<Engagement> entities)
        {
            if (entities == null)
            {
                return false;
            }

            var allSuccessful = true;
            foreach (var entity in entities)
            {
                var wasSuccessful = await SaveAsync(entity);
                if (wasSuccessful == false)
                {
                    allSuccessful = false;
                }
            }

            return allSuccessful;
        }

    }
}