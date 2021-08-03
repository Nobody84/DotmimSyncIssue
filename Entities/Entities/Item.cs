using System;

namespace DataEntities.Entities
{
    public class Item
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public DateTime Created { get; set; }

        public DateTime LastModified { get; set; }
    }
}
