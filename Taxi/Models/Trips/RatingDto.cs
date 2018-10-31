using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Taxi.Models.Trips
{
    public class RatingDto
    {
        private int _rating = 0;

        private int _maxRating = 5;

        private int _minRating = 1;

        public int Rating
        {
            get => _rating;
            set
            {
                _rating = value;
                if (value > _maxRating)
                    _rating = _maxRating;
                if (value < _minRating)
                    _rating = _minRating;
            }  
        }        
    }
}
