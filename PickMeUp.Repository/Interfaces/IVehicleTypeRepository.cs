﻿using PickMeUp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickMeUp.Repository.Interfaces
{
    public  interface IVehicleTypeRepository : IRepository<VehicleType>
    {
        VehicleType GetVehicleByName(string vehicleType);
    }
}
