
interface PlayerMessagesProps {
    messages: string[];
}
const PlayerMessages : React.FC<PlayerMessagesProps> = ({messages}) => {
    return (
        <div key="player-messages" className="player-messages">
            <p>Player Messages:</p>
            <ul>
                {messages.map((message, index) => (
                    <li key={index}>{message}</li>
                ))}
            </ul>
        </div>
    );
};
export default PlayerMessages;
