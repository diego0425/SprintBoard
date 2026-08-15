import { ArrowLeft } from "lucide-react";
import { useNavigate } from "react-router-dom";

export default function BackButton() {
  const navigate = useNavigate();

  function handleBack() {
    if (window.history.length > 1){
        navigate(-1);
    } else {
        navigate("/boards")
    }
  }

  return (
    <button className="back-button" onClick={handleBack}>
      <ArrowLeft size={16} />
    </button>
  );
}