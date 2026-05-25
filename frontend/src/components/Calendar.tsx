import { useState, useEffect } from 'react';
import '../styles/calendar.css';
import BloodDropIcon from './BloodDropIcon';

interface Cycle {
  id: string;
  startDate: string;
  durationDays: number;
  createdAt: string;
  corrected: boolean;
}

interface Prediction {
  id: string;
  predictedStart: string;
  predictedDuration: number;
  confidence: number;
}

interface CalendarProps {
  cycles: Cycle[];
  onCycleAdded: () => void;
  userId: string;
}

function Calendar({ cycles, onCycleAdded, userId }: CalendarProps) {
  const [currentMonth, setCurrentMonth] = useState(new Date());
  const [predictions, setPredictions] = useState<Prediction[]>([]);

  useEffect(() => {
    fetchPredictions();
  }, [userId]);

  const fetchPredictions = async () => {
    try {
      const res = await fetch(`/api/user/${userId}/predict?cycles=15`);
      if (res.ok) {
        const data = await res.json();
        setPredictions(data);
      }
    } catch (error) {
      console.error('Error fetching predictions:', error);
    }
  };

  const handleAddCycle = async (date: Date) => {
    const startDate = date.toISOString().split('T')[0];
    const durationDays = 5;

    try {
      const res = await fetch(`/api/user/${userId}/cycles`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ startDate, durationDays })
      });

      if (res.ok) {
        onCycleAdded();
        fetchPredictions();
      }
    } catch (error) {
      console.error('Error adding cycle:', error);
    }
  };

  const getDaysInMonth = (date: Date) => {
    return new Date(date.getFullYear(), date.getMonth() + 1, 0).getDate();
  };

  const getFirstDayOfMonth = (date: Date) => {
    return new Date(date.getFullYear(), date.getMonth(), 1).getDay();
  };

  const getCycleForDate = (date: Date) => {
    return cycles.find(c => {
      const cycleStart = new Date(c.startDate);
      const cycleDates = [];
      for (let i = 0; i < c.durationDays; i++) {
        const d = new Date(cycleStart);
        d.setDate(d.getDate() + i);
        cycleDates.push(d.toDateString());
      }
      return cycleDates.includes(date.toDateString());
    });
  };

  const getPredictionForDate = (date: Date) => {
    return predictions.find(p => {
      const predStart = new Date(p.predictedStart);
      const predDates = [];
      for (let i = 0; i < p.predictedDuration; i++) {
        const d = new Date(predStart);
        d.setDate(d.getDate() + i);
        predDates.push(d.toDateString());
      }
      return predDates.includes(date.toDateString());
    });
  };

  const getPredictionColor = (index: number) => {
    if (index === 0) return '#ff6b6b';
    if (index === 1) return '#ffb86b';
    return '#ffd966';
  };

  const renderCalendar = () => {
    const daysInMonth = getDaysInMonth(currentMonth);
    const firstDay = getFirstDayOfMonth(currentMonth);
    const days = [];

    for (let i = 0; i < firstDay; i++) {
      days.push(<div key={`empty-${i}`} className="calendar-day empty"></div>);
    }

    for (let day = 1; day <= daysInMonth; day++) {
      const date = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day);
      const cycle = getCycleForDate(date);
      const prediction = getPredictionForDate(date);

      days.push(
        <div key={day} className="calendar-day" onClick={() => handleAddCycle(date)}>
          <span className="day-number">{day}</span>
          {cycle && (
            <div className="period-mark actual">
              <BloodDropIcon color="#ff6b6b" />
            </div>
          )}
          {prediction && !cycle && (
            <div className="period-mark predicted" style={{ color: getPredictionColor(predictions.indexOf(prediction)) }}>
              <BloodDropIcon color={getPredictionColor(predictions.indexOf(prediction))} />
            </div>
          )}
        </div>
      );
    }

    return days;
  };

  const monthName = currentMonth.toLocaleString('default', { month: 'long', year: 'numeric' });

  return (
    <div className="calendar">
      <div className="calendar-header">
        <button onClick={() => setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1))}>
          ← Previous
        </button>
        <h2>{monthName}</h2>
        <button onClick={() => setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1))}>
          Next →
        </button>
      </div>

      <div className="calendar-weekdays">
        <div>Sun</div>
        <div>Mon</div>
        <div>Tue</div>
        <div>Wed</div>
        <div>Thu</div>
        <div>Fri</div>
        <div>Sat</div>
      </div>

      <div className="calendar-grid">
        {renderCalendar()}
      </div>

      <div className="calendar-legend">
        <div className="legend-item">
          <BloodDropIcon color="#ff6b6b" /> Actual Period
        </div>
        <div className="legend-item">
          <BloodDropIcon color="#ffb86b" /> Next Predicted
        </div>
        <div className="legend-item">
          <BloodDropIcon color="#ffd966" /> Future Predicted
        </div>
      </div>
    </div>
  );
}

export default Calendar;
