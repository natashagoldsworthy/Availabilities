using System;
using System.Collections.Generic;
using System.Linq;
using Availabilities.Other;
using Availabilities.Resources;
using Availabilities.Storage;

namespace Availabilities.Apis.Application
{
    internal class AvailabilitiesApplication : IAvailabilitiesApplication
    {
        private readonly IStorage<Availability> storage;

        public AvailabilitiesApplication(IStorage<Availability> storage)
        {
            this.storage = storage;
        }

        public TimeSlot ReserveAvailability(TimeSlot slot)
        {
            List<Availability> availabilities = this.storage.List();
            DateTime bookingStartTime = slot.Start.ToNextOrCurrentQuarterHour();
            DateTime bookingEndTime = slot.End.ToNextOrCurrentQuarterHour();
            Availability availability =
                availabilities.SingleOrDefault(a => a.StartUtc <= bookingStartTime && a.EndUtc >= bookingEndTime);
            if (availability != null)
            {
                if (availability.StartUtc == bookingStartTime && availability.EndUtc == bookingEndTime)
                {
                    this.storage.Delete(availability.Id);
                }
                else if (availability.StartUtc == bookingStartTime)
                {
                    availability.StartUtc = bookingEndTime;
                    this.storage.Upsert(availability);
                }
                else if (availability.EndUtc == bookingEndTime)
                {
                    availability.EndUtc = bookingStartTime;
                    this.storage.Upsert(availability);
                }
                else
                {
                    Availability newAvailability = new Availability
                    {
                        StartUtc = bookingEndTime,
                        EndUtc = availability.EndUtc
                    };
                    availability.EndUtc = bookingStartTime;
                    this.storage.Upsert(availability);
                    this.storage.Upsert(newAvailability);
                }
            }
            else
            {
                throw new ResourceConflictException("The booking cannot be made for this time period");
            }

            return new TimeSlot(bookingStartTime, bookingEndTime);
        }

        public void ReleaseAvailability(TimeSlot slot)
        {
            DateTime bookingStartTime = slot.Start.ToNextOrCurrentQuarterHour();
            DateTime bookingEndTime = slot.End.ToNextOrCurrentQuarterHour();
            List<Availability> availabilities = this.storage.List();
            Availability firstAvailability = availabilities.SingleOrDefault(availability => availability.EndUtc == bookingStartTime);
            Availability secondAvailability = availabilities.SingleOrDefault(availability => availability.StartUtc == bookingEndTime);
            if (firstAvailability != null && secondAvailability != null)
            {
                firstAvailability.EndUtc = secondAvailability.EndUtc;
                this.storage.Delete(secondAvailability.Id);
                this.storage.Upsert(firstAvailability);
            }
            else
            {
                if (firstAvailability != null)
                {
                    firstAvailability.EndUtc = bookingEndTime;
                    this.storage.Upsert(firstAvailability);
                }
                else if (secondAvailability != null)
                {
                    secondAvailability.StartUtc = bookingStartTime;
                    this.storage.Upsert(secondAvailability);
                }
                else
                {
                    Availability newAvailability = new Availability
                    {
                        StartUtc = bookingStartTime,
                        EndUtc = bookingEndTime
                    };
                    this.storage.Upsert(newAvailability);
                }
            }
        }
    }
}