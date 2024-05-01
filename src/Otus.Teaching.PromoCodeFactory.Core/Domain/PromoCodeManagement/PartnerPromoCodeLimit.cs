using System;

namespace Otus.Teaching.PromoCodeFactory.Core.Domain.PromoCodeManagement
{
    public class PartnerPromoCodeLimit
    {
        public Guid Id { get; set; }

        public Guid PartnerId { get; set; }

        public virtual Partner Partner { get; set; }
        //дата создания лимита
        public DateTime CreateDate { get; set; }
        //дата отмены лимита
        public DateTime? CancelDate { get; set; }
        //дата окончания лимита
        public DateTime EndDate { get; set; }

        public int Limit { get; set; }
    }
}