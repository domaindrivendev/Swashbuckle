﻿using System;
using System.Collections.Generic;
using System.Web.Http;

namespace Swashbuckle.Dummy.Controllers
{
    public class NestedEnumerableTypesController : ApiController
    {
        public int Create(Matrix matrix)
        {
            throw new NotImplementedException();
        }
    }

    public class Matrix : List<List<int>>
    {
    }
}